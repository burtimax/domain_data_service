namespace Shared.Configs;

public class AppConfiguration
{
    public string WebAppUrl { get; set; } = string.Empty;
    public DatabaseAppConfiguration Database { get; set; } = new();
    public ReceiverConfiguration Receiver { get; set; } = new();
    public RabbitMqConfiguration RabbitMq { get; set; } = new();
}