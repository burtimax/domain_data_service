namespace Shared.Configs;

public class RabbitMqConfiguration
{
    public bool EnableConsumer { get; set; } = true;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ObservationExchange { get; set; } = "receiver.observations.exchange";
    public string ObservationRoutingKey { get; set; } = "receiver.observation.v1";
    public string ObservationQueue { get; set; } = "receiver.observations.v1";
    public ushort PrefetchCount { get; set; } = 10;
    public string DlqExchange { get; set; } = "receiver.observations.dlx";
    public string DlqRoutingKey { get; set; } = "receiver.observation.v1.dlq";
    public string DlqQueue { get; set; } = "receiver.observations.v1.dlq";
}
