namespace ConsoleTest.RabbitMQTestObservations;

public sealed class RabbitMqObservationScenarioRunner
{
    private readonly RabbitMqTestOptions _options;
    private readonly RabbitMqObservationPublisher _publisher;

    public RabbitMqObservationScenarioRunner(RabbitMqTestOptions options)
    {
        _options = options;
        _publisher = new RabbitMqObservationPublisher(options);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var messages = TestObservationFactory.Build(_options.MessagesCount);
        await _publisher.PublishAsync(messages, ct);
        Console.WriteLine($"Published {messages.Count} test observation messages to {_options.Exchange}:{_options.RoutingKey} (queue: {_options.Queue})");
    }
}
