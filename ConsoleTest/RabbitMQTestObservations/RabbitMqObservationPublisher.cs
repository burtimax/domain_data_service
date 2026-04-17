using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Shared.Contracts.Ingestion;

namespace ConsoleTest.RabbitMQTestObservations;

public sealed class RabbitMqObservationPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqTestOptions _options;

    public RabbitMqObservationPublisher(RabbitMqTestOptions options)
    {
        _options = options;
    }

    public async Task PublishAsync(IEnumerable<ObservationIngestionMessage> messages, CancellationToken ct)
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
        await channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Direct, durable: true, cancellationToken: ct);

        foreach (var message in messages)
        {
            var payload = JsonSerializer.Serialize(message, JsonOptions);
            var body = Encoding.UTF8.GetBytes(payload);

            await channel.BasicPublishAsync(
                exchange: _options.Exchange,
                routingKey: _options.RoutingKey,
                mandatory: false,
                basicProperties: new BasicProperties { Persistent = true },
                body: body,
                cancellationToken: ct);
        }
    }
}
