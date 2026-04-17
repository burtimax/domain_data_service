namespace Shared.Configs;

public class AppConfiguration
{
    public string WebAppUrl { get; set; }
    public DatabaseAppConfiguration Database { get; set; }
    public SmsGatewayConfiguration SMSGateway { get; set; }
}