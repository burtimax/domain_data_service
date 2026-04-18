namespace ParserNicsell.Ingestion;

public sealed class RabbitMqPublishOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public string Exchange { get; set; } = "receiver.observations.exchange";
    public string RoutingKey { get; set; } = "receiver.observation.v1";
    public string Queue { get; set; } = "receiver.observations.v1";
    public string DlqExchange { get; set; } = "receiver.observations.dlx";
    public string DlqRoutingKey { get; set; } = "receiver.observation.v1.dlq";
    public bool DeclareTopologyBeforePublish { get; set; } = true;

    /// <summary>Задержка между сообщениями, мс (0 — без паузы).</summary>
    public int PublishDelayMs { get; set; }
}
