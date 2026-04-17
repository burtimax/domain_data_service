using System.Text;
using System.Text.Json;
using Api.Services;
using Application.Services.Ingestion;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Configs;
using Shared.Contracts.Ingestion;

namespace Api.BackgroundServices;

public sealed class RabbitMqObservationConsumerService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqObservationConsumerService> _logger;
    private readonly AppConfiguration _config;
    private readonly IngestionMetrics _metrics;
    private readonly IFailedMessageArchiveStore _archiveStore;

    private IConnection? _connection;
    private IChannel? _channel;
    private string? _consumerTag;

    public RabbitMqObservationConsumerService(
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqObservationConsumerService> logger,
        AppConfiguration config,
        IngestionMetrics metrics,
        IFailedMessageArchiveStore archiveStore)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
        _metrics = metrics;
        _archiveStore = archiveStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mq = _config.RabbitMq;
        if (!mq.EnableConsumer)
        {
            _logger.LogInformation("RabbitMQ ingestion consumer disabled by config.");
            return;
        }

        var factory = new ConnectionFactory
        {
            HostName = mq.Host,
            Port = mq.Port,
            VirtualHost = mq.VirtualHost,
            UserName = mq.Username,
            Password = mq.Password,
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await _channel.BasicQosAsync(0, mq.PrefetchCount, false, stoppingToken);

        await DeclareTopologyAsync(_channel, mq, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) => await ProcessMessageAsync(ea, stoppingToken);

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: mq.ObservationQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "RabbitMQ observation consumer started. exchange={Exchange}, queue={Queue}, dlq={DlqQueue}",
            mq.ObservationExchange,
            mq.ObservationQueue,
            mq.DlqQueue);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            var snapshot = _metrics.Snapshot();
            _logger.LogInformation(
                "Ingestion metrics: received={Received}, acked={Acked}, duplicates={Duplicates}, retried={Retried}, requeued={Requeued}, dlq={DeadLettered}, failed={Failed}, throughput={Throughput}/s, avgLagMs={AvgLagMs}",
                snapshot.Received,
                snapshot.Acked,
                snapshot.Duplicates,
                snapshot.Retried,
                snapshot.Requeued,
                snapshot.DeadLettered,
                snapshot.Failed,
                snapshot.ThroughputPerSecond,
                snapshot.AverageLagMs);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_channel is not null && _consumerTag is not null)
            {
                await _channel.BasicCancelAsync(_consumerTag, noWait: false, cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cancel RabbitMQ consumer cleanly.");
        }

        if (_channel is IAsyncDisposable asyncChannel)
            await asyncChannel.DisposeAsync();
        if (_connection is IAsyncDisposable asyncConnection)
            await asyncConnection.DisposeAsync();

        await base.StopAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        _metrics.IncrementReceived();
        var bodyText = Encoding.UTF8.GetString(ea.Body.ToArray());
        var retryAttempt = GetRetryAttempt(ea.BasicProperties.Headers);
        string? messageId = null;

        try
        {
            var message = JsonSerializer.Deserialize<ObservationIngestionMessage>(bodyText, JsonOptions)
                          ?? throw new NonRetryableIngestionException("Message body is invalid JSON.");
            messageId = message.MessageId;
            _metrics.RecordLag(message.ObservedAt.ToUniversalTime());
            using var scopeLog = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["MessageId"] = message.MessageId,
                ["DeliveryTag"] = ea.DeliveryTag,
                ["RetryAttempt"] = retryAttempt
            });

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IObservationIngestionHandler>();

            await handler.HandleAsync(message, IngestionSource.RabbitMq, cancellationToken);
            await _channel!.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
            _metrics.IncrementAcked();
        }
        catch (NonRetryableIngestionException ex)
        {
            _metrics.IncrementFailed();
            _metrics.IncrementDeadLettered();
            _logger.LogWarning(ex, "Non-retryable ingestion error. Message moved to DLQ.");
            await ArchiveFailedMessageAsync("non_retryable", messageId, retryAttempt, bodyText, cancellationToken);
            await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: false, cancellationToken);
        }
        catch (RetryableIngestionException ex)
        {
            await HandleRetryableFailureAsync(ea, ex, retryAttempt, messageId, bodyText, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleRetryableFailureAsync(ea, ex, retryAttempt, messageId, bodyText, cancellationToken);
        }
    }

    private async Task HandleRetryableFailureAsync(
        BasicDeliverEventArgs ea,
        Exception ex,
        int retryAttempt,
        string? messageId,
        string bodyText,
        CancellationToken cancellationToken)
    {
        _metrics.IncrementFailed();
        var maxRetryAttempts = _config.RabbitMq.MaxRetryAttempts;

        if (retryAttempt >= maxRetryAttempts)
        {
            _metrics.IncrementDeadLettered();
            _logger.LogError(ex, "Retryable ingestion error exceeded max retry attempts ({MaxRetryAttempts}). Message moved to DLQ.", maxRetryAttempts);
            await ArchiveFailedMessageAsync("retry_exhausted", messageId, retryAttempt, bodyText, cancellationToken);
            await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: false, cancellationToken);
            return;
        }

        var headers = CloneHeaders(ea.BasicProperties.Headers);
        headers["x-retry-count"] = retryAttempt + 1;

        await _channel!.BasicPublishAsync(
            exchange: _config.RabbitMq.ObservationExchange,
            routingKey: _config.RabbitMq.ObservationRoutingKey,
            mandatory: false,
            basicProperties: new BasicProperties
            {
                Persistent = true,
                Headers = headers
            },
            body: ea.Body,
            cancellationToken: cancellationToken);

        await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
        _metrics.IncrementRetried();
        _metrics.IncrementRequeued();
        _logger.LogWarning(ex, "Retryable ingestion error. Message rescheduled for retry {RetryAttempt}/{MaxRetryAttempts}.", retryAttempt + 1, maxRetryAttempts);
    }

    private static int GetRetryAttempt(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue("x-retry-count", out var value) || value is null)
            return 0;

        return value switch
        {
            int i => i,
            long l => (int)l,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            _ => 0
        };
    }

    private static Dictionary<string, object?> CloneHeaders(IDictionary<string, object?>? headers)
    {
        if (headers is null)
            return new Dictionary<string, object?>();

        return headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private Task ArchiveFailedMessageAsync(string reason, string? messageId, int retryAttempt, string payload, CancellationToken ct)
    {
        return _archiveStore.AppendAsync(new FailedMessageArchiveRecord
        {
            FailedAtUtc = DateTimeOffset.UtcNow,
            Reason = reason,
            MessageId = messageId,
            RetryAttempt = retryAttempt,
            Payload = payload
        }, ct);
    }

    private static async Task DeclareTopologyAsync(IChannel channel, RabbitMqConfiguration mq, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(mq.ObservationExchange, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(mq.DlqExchange, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);

        var queueArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = mq.DlqExchange,
            ["x-dead-letter-routing-key"] = mq.DlqRoutingKey,
        };

        await channel.QueueDeclareAsync(
            queue: mq.ObservationQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: mq.ObservationQueue,
            exchange: mq.ObservationExchange,
            routingKey: mq.ObservationRoutingKey,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: mq.DlqQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: mq.DlqQueue,
            exchange: mq.DlqExchange,
            routingKey: mq.DlqRoutingKey,
            cancellationToken: cancellationToken);
    }
}
