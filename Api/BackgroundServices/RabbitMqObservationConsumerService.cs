using System.Text;
using System.Text.Json;
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

    private IConnection? _connection;
    private IChannel? _channel;
    private string? _consumerTag;

    public RabbitMqObservationConsumerService(
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqObservationConsumerService> logger,
        AppConfiguration config,
        IngestionMetrics metrics)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
        _metrics = metrics;
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
                "Ingestion metrics: received={Received}, acked={Acked}, requeued={Requeued}, dlq={DeadLettered}, failed={Failed}",
                snapshot.Received,
                snapshot.Acked,
                snapshot.Requeued,
                snapshot.DeadLettered,
                snapshot.Failed);
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

        try
        {
            var bodyText = Encoding.UTF8.GetString(ea.Body.ToArray());
            var message = JsonSerializer.Deserialize<ObservationIngestionMessage>(bodyText, JsonOptions)
                          ?? throw new NonRetryableIngestionException("Message body is invalid JSON.");

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
            await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: false, cancellationToken);
        }
        catch (RetryableIngestionException ex)
        {
            await HandleRetryableFailureAsync(ea, ex, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleRetryableFailureAsync(ea, ex, cancellationToken);
        }
    }

    private async Task HandleRetryableFailureAsync(
        BasicDeliverEventArgs ea,
        Exception ex,
        CancellationToken cancellationToken)
    {
        _metrics.IncrementFailed();

        if (ea.Redelivered)
        {
            _metrics.IncrementDeadLettered();
            _logger.LogError(ex, "Retryable ingestion error after redelivery. Message moved to DLQ.");
            await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: false, cancellationToken);
            return;
        }

        _metrics.IncrementRequeued();
        _logger.LogWarning(ex, "Retryable ingestion error. Message requeued.");
        await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: true, cancellationToken);
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
