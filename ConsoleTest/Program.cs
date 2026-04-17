using System.Text.Json;
using ConsoleTest.NicsellParser;
using ConsoleTest.NicsellParser.Models;
using Microsoft.Extensions.Logging;


var cookie = "nicsell_session=vri5s9ikov8eg9bhdobsi6ni6d; liveauction-infobox-warning-time-offset-off=1";
if (string.IsNullOrWhiteSpace(cookie))
{
    Console.WriteLine($"Укажи cookie в переменной окружения NICSELL_COOKIE.");
    Console.WriteLine($"Пример: nicsell_session=...; liveauction-infobox-warning-time-offset-off=1");
    Console.WriteLine($"Уровень логов: NICSELL_LOG_LEVEL (Information, Debug, Warning, …).");
    return;
}

var options = new NicsellParserOptions
{
    CookieHeader = cookie,
    MaxPerPage = 250,
    MaxPages = 50
};

var filter = new NicsellFilter
{
    LengthTo = null,
    Sort = "bid_desc",
    Option = null
};

using var httpClient = new HttpClient();
var parser = new NicsellDomainParser(httpClient, options);

var result = await parser.ParseAsync(filter);
var outputDir = Path.Combine(AppContext.BaseDirectory, "output");
Directory.CreateDirectory(outputDir);

var filePath = Path.Combine(outputDir, $"nicsell-domains-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
{
    WriteIndented = true
});
await File.WriteAllTextAsync(filePath, json);

Console.WriteLine($"Собрано доменов: {result.Items.Count}");
Console.WriteLine($"Пройдено страниц: {result.VisitedPages}");
if (result.Pagination is { } p)
{
    Console.WriteLine($"Пагинация: текущая страница={p.CurrentPage?.ToString() ?? "?"}, всего страниц={p.TotalPages?.ToString() ?? "?"}, записей на странице (из HTML)={p.EntriesPerPageFromHtml?.ToString() ?? "?"}, примерно всего записей={p.ApproximateTotalEntries?.ToString() ?? "?"}.");
}

Console.WriteLine($"Файл результата: {filePath}");

Console.WriteLine();
Console.WriteLine($"Первые 10 доменов:");
foreach (var item in result.Items.Take(10))
{
    var bid = item.CurrentBidEur is { } b ? b.ToString("0.##") : "-";
    var end = item.AuctionEndsAt is { } e ? e.ToString("u") : "-";
    var tfCf = item.MajesticTfCfRatio is { } r ? r.ToString("0.##") : "-";
    Console.WriteLine($"{item.Domain,-35} bid={bid}, bids={item.BidCount?.ToString() ?? "-"}, end={end}, inTld={item.InTldCount?.ToString() ?? "-"}, RD={item.MajesticRefDomains?.ToString() ?? "-"}, BL={item.MajesticBacklinks?.ToString() ?? "-"}, TF={item.MajesticTrustFlow?.ToString() ?? "-"}, CF={item.MajesticCitationFlow?.ToString() ?? "-"}, TF/CF={tfCf}, google={item.GoogleHits?.ToString() ?? "-"}, ARC={item.ArchiveOrgResults?.ToString() ?? "-"}");
}
