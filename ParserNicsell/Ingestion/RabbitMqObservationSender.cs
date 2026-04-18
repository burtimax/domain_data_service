using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Shared.Contracts.Ingestion;

namespace ParserNicsell.Ingestion;

public sealed class RabbitMqObservationSender
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqPublishOptions _options;

    public RabbitMqObservationSender(RabbitMqPublishOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(IEnumerable<ObservationIngestionMessage> messages, CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.Username,
            Password = _options.Password
        };

        await using var connection = await factory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        if (_options.DeclareTopologyBeforePublish)
        {
            await channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Direct, durable: true, cancellationToken: ct);
            await EnsureQueueExistsAsync(channel, ct);
            await channel.QueueBindAsync(
                queue: _options.Queue,
                exchange: _options.Exchange,
                routingKey: _options.RoutingKey,
                cancellationToken: ct);
        }

        foreach (var message in messages)
        {
            var payload = JsonSerializer.Serialize(message, JsonOptions);
            var body = Encoding.UTF8.GetBytes(payload);

            await channel.BasicPublishAsync(
                exchange: _options.Exchange,
                routingKey: _options.RoutingKey,
                mandatory: true,
                basicProperties: new BasicProperties { Persistent = true },
                body: body,
                cancellationToken: ct);

            // if (_options.PublishDelayMs > 0)
            // {
            //     await Task.Delay(_options.PublishDelayMs, ct);
            // }
        }
    }

    private async Task EnsureQueueExistsAsync(IChannel channel, CancellationToken ct)
    {
        try
        {
            await channel.QueueDeclarePassiveAsync(_options.Queue, ct);
            return;
        }
        catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == 404)
        {
        }

        var args = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = _options.DlqExchange,
            ["x-dead-letter-routing-key"] = _options.DlqRoutingKey
        };

        await channel.QueueDeclareAsync(
            queue: _options.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args,
            cancellationToken: ct);
    }
}
