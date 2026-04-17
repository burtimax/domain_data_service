namespace Shared.Configs;

public class ReceiverConfiguration
{
    public bool EnableHttpIngestion { get; set; }
    public int MaxAcceptedClockSkewMinutes { get; set; } = 5;
    public string[] AllowedOrigins { get; set; } = [];
}
