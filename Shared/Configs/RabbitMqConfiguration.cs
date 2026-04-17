namespace Shared.Configs;

public class RabbitMqConfiguration
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ObservationQueue { get; set; } = "receiver.observations.v1";
}
