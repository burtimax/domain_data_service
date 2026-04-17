namespace ConsoleTest.RabbitMQTestObservations;

public sealed class RabbitMqTestOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public string Exchange { get; set; } = "receiver.observations.exchange";
    public string RoutingKey { get; set; } = "receiver.observation.v1";
    public int MessagesCount { get; set; } = 10;
}
