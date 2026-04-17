using System.Text;
using RabbitMQ.Client;
using Shared.Configs;

namespace Api.Services;

public interface IRabbitMqOpsService
{
    Task<QueueStatusSnapshot> GetQueueStatusAsync(CancellationToken ct);
    Task<ReplayResult> ReplayFromDlqAsync(int maxMessages, CancellationToken ct);
    Task<ReplayResult> ReplayFromArchiveAsync(int maxMessages, CancellationToken ct);
}

public sealed class RabbitMqOpsService : IRabbitMqOpsService
{
    private readonly AppConfiguration _config;
    private readonly IFailedMessageArchiveStore _archiveStore;

    public RabbitMqOpsService(AppConfiguration config, IFailedMessageArchiveStore archiveStore)
    {
        _config = config;
        _archiveStore = archiveStore;
    }

    public async Task<QueueStatusSnapshot> GetQueueStatusAsync(CancellationToken ct)
    {
        await using var connection = await CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        var main = await channel.QueueDeclarePassiveAsync(_config.RabbitMq.ObservationQueue, ct);
        var dlq = await channel.QueueDeclarePassiveAsync(_config.RabbitMq.DlqQueue, ct);
        return new QueueStatusSnapshot
        {
            ObservationQueue = _config.RabbitMq.ObservationQueue,
            DlqQueue = _config.RabbitMq.DlqQueue,
            ObservationQueueDepth = main.MessageCount,
            DlqQueueDepth = dlq.MessageCount
        };
    }

    public async Task<ReplayResult> ReplayFromDlqAsync(int maxMessages, CancellationToken ct)
    {
        await using var connection = await CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        var replayed = 0;
        var failed = 0;

        for (var i = 0; i < maxMessages; i++)
        {
            var message = await channel.BasicGetAsync(_config.RabbitMq.DlqQueue, autoAck: false, cancellationToken: ct);
            if (message is null)
                break;

            try
            {
                await PublishToObservationAsync(channel, message.Body.ToArray(), message.BasicProperties.Headers, ct);
                await channel.BasicAckAsync(message.DeliveryTag, false, ct);
                replayed++;
            }
            catch
            {
                failed++;
                await channel.BasicNackAsync(message.DeliveryTag, false, requeue: true, ct);
            }
        }

        return new ReplayResult { Requested = maxMessages, Replayed = replayed, Failed = failed };
    }

    public async Task<ReplayResult> ReplayFromArchiveAsync(int maxMessages, CancellationToken ct)
    {
        var replayed = 0;
        var failed = 0;
        var records = await _archiveStore.ReadRecentAsync(maxMessages, ct);
        await using var connection = await CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        foreach (var record in records)
        {
            try
            {
                await PublishToObservationAsync(channel, Encoding.UTF8.GetBytes(record.Payload), null, ct);
                replayed++;
            }
            catch
            {
                failed++;
            }
        }

        return new ReplayResult { Requested = maxMessages, Replayed = replayed, Failed = failed };
    }

    private async Task<IConnection> CreateConnectionAsync(CancellationToken ct)
    {
        var mq = _config.RabbitMq;
        var factory = new ConnectionFactory
        {
            HostName = mq.Host,
            Port = mq.Port,
            VirtualHost = mq.VirtualHost,
            UserName = mq.Username,
            Password = mq.Password
        };
        return await factory.CreateConnectionAsync(ct);
    }

    private async Task PublishToObservationAsync(IChannel channel, byte[] body, IDictionary<string, object?>? headers, CancellationToken ct)
    {
        var props = new BasicProperties
        {
            Persistent = true,
            Headers = headers is null ? new Dictionary<string, object?>() : new Dictionary<string, object?>(headers)
        };
        props.Headers.Remove("x-retry-count");
        await channel.BasicPublishAsync(
            exchange: _config.RabbitMq.ObservationExchange,
            routingKey: _config.RabbitMq.ObservationRoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }
}

public sealed class QueueStatusSnapshot
{
    public string ObservationQueue { get; set; } = string.Empty;
    public string DlqQueue { get; set; } = string.Empty;
    public uint ObservationQueueDepth { get; set; }
    public uint DlqQueueDepth { get; set; }
}

public sealed class ReplayResult
{
    public int Requested { get; set; }
    public int Replayed { get; set; }
    public int Failed { get; set; }
}
