using ConsoleTest.RabbitMQTestObservations;
using Microsoft.Extensions.Configuration;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var options = new RabbitMqTestOptions();
configuration.GetSection("RabbitMq").Bind(options);

Console.WriteLine($"Конфигурация: окружение={environment}, RabbitMQ={options.Host}:{options.Port}, сообщений={options.MessagesCount}");
Console.WriteLine("Starting RabbitMQ test observation scenario...");
var runner = new RabbitMqObservationScenarioRunner(options);
await runner.RunAsync(CancellationToken.None);
Console.WriteLine("Done.");
