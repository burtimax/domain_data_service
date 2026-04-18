using System.Reflection;
using System.Text.Json;
using ConsoleTest.NicsellParser;
using ConsoleTest.NicsellParser.Models;
using Microsoft.Extensions.Configuration;
using ParserNicsell.Ingestion;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var nicsellSection = configuration.GetSection("Nicsell");
var cookieHeader = nicsellSection["CookieHeader"] ?? string.Empty;
if (string.IsNullOrWhiteSpace(cookieHeader))
{
    Console.WriteLine("Не задан Nicsell:CookieHeader.");
    Console.WriteLine("Укажите в appsettings.json / appsettings.Development.json (секция Nicsell) или переопределите переменной окружения Nicsell__CookieHeader.");
    Console.WriteLine("Окружение конфигурации: DOTNET_ENVIRONMENT (по умолчанию Production → appsettings.json). Для Development: set DOTNET_ENVIRONMENT=Development");
    return;
}

var sendToRabbit = configuration.GetSection("Run").GetValue("SendToRabbitMq", true);
var saveJson = configuration.GetSection("Run").GetValue("SaveJsonOutput", true);

var rabbitOptions = new RabbitMqPublishOptions();
configuration.GetSection("RabbitMq").Bind(rabbitOptions);

var options = new NicsellParserOptions
{
    CookieHeader = cookieHeader,
    MaxPerPage = nicsellSection.GetValue("MaxPerPage", 250),
    MaxPages = nicsellSection.GetValue<int?>("MaxPages")
};

var filter = new NicsellFilter
{
    LengthTo = null,
    Sort = nicsellSection["Sort"] ?? "bid_desc",
    Option = null
};

using var httpClient = new HttpClient();
var parser = new NicsellDomainParser(httpClient, options);

Console.WriteLine($"Конфигурация: окружение={environment}, отправка в RabbitMQ={sendToRabbit}, JSON на диск={saveJson}");
Console.WriteLine("Парсинг списка доменов Nicsell...");
var result = await parser.ParseAsync(filter);

var obsSection = configuration.GetSection("ObservationPublishing");
var parserCode = obsSection["ParserCode"] ?? "parser-nicsell";
var parserVersion = obsSection["ParserVersion"];
if (string.IsNullOrWhiteSpace(parserVersion))
{
    parserVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
}

var observedAt = result.ParsedAtUtc;

if (saveJson)
{
    var outputDir = Path.Combine(AppContext.BaseDirectory, "output");
    Directory.CreateDirectory(outputDir);
    var filePath = Path.Combine(outputDir, $"nicsell-domains-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
    var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(filePath, json);
    Console.WriteLine($"Файл результата: {filePath}");
}

Console.WriteLine($"Собрано доменов: {result.Items.Count}");
Console.WriteLine($"Пройдено страниц: {result.VisitedPages}");
if (result.Pagination is { } p)
{
    Console.WriteLine(
        $"Пагинация: текущая страница={p.CurrentPage?.ToString() ?? "?"}, всего страниц={p.TotalPages?.ToString() ?? "?"}, записей на странице (из HTML)={p.EntriesPerPageFromHtml?.ToString() ?? "?"}, примерно всего записей={p.ApproximateTotalEntries?.ToString() ?? "?"}.");
}

if (sendToRabbit)
{
    Console.WriteLine($"Отправка в RabbitMQ {rabbitOptions.Host}:{rabbitOptions.Port}, exchange={rabbitOptions.Exchange}, routingKey={rabbitOptions.RoutingKey} ...");
    var messages = result.Items
        .Select(r => NicsellObservationMapper.ToObservation(r, parserCode, parserVersion, observedAt))
        .ToList();

    var sender = new RabbitMqObservationSender(rabbitOptions);
    await sender.SendAsync(messages, CancellationToken.None);
    Console.WriteLine($"В очередь отправлено сообщений: {messages.Count}.");
}
else
{
    Console.WriteLine("Отправка в RabbitMQ отключена (Run:SendToRabbitMq=false).");
}

Console.WriteLine();
Console.WriteLine("Первые 10 доменов:");
foreach (var item in result.Items.Take(10))
{
    var bid = item.CurrentBidEur is { } b ? b.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : "-";
    var end = item.AuctionEndsAt is { } e ? e.ToString("u") : "-";
    var tfCf = item.MajesticTfCfRatio is { } r ? r.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : "-";
    Console.WriteLine(
        $"{item.Domain,-35} bid={bid}, bids={item.BidCount?.ToString() ?? "-"}, end={end}, inTld={item.InTldCount?.ToString() ?? "-"}, RD={item.MajesticRefDomains?.ToString() ?? "-"}, BL={item.MajesticBacklinks?.ToString() ?? "-"}, TF={item.MajesticTrustFlow?.ToString() ?? "-"}, CF={item.MajesticCitationFlow?.ToString() ?? "-"}, TF/CF={tfCf}, google={item.GoogleHits?.ToString() ?? "-"}, ARC={item.ArchiveOrgResults?.ToString() ?? "-"}");
}

Console.WriteLine("Готово.");
