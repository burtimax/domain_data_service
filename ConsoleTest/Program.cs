using ConsoleTest.RabbitMQTestObservations;

var options = new RabbitMqTestOptions
{
    Host =  "localhost",
    // 5672 = AMQP (broker), 15672/15671 = management UI.
    Port = 5672,
    VirtualHost =  "/",
    Username =  "tim",
    Password = "123",
    Exchange = "receiver.observations.exchange",
    RoutingKey = "receiver.observation.v1",
    MessagesCount =  10
};

Console.WriteLine("Starting RabbitMQ test observation scenario...");
var runner = new RabbitMqObservationScenarioRunner(options);
await runner.RunAsync(CancellationToken.None);
Console.WriteLine("Done.");
