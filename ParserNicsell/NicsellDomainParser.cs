using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using ConsoleTest.NicsellParser.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConsoleTest.NicsellParser;

public sealed class NicsellDomainParser
{
    private static readonly Regex DomainRegex = new(@"^[\p{L}\p{Nd}-]+\.[\p{L}\p{Nd}-]+$", RegexOptions.Compiled);
    private readonly HttpClient _httpClient;
    private readonly NicsellParserOptions _options;

    public NicsellDomainParser(HttpClient httpClient, NicsellParserOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    private static readonly Regex PageQueryRegex = new(@"[?&]page=(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MaxPerPageQueryRegex = new(@"maxperpage=(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<NicsellParseResult> ParseAsync(NicsellFilter filter, CancellationToken cancellationToken = default)
    {
        EnsureClientInitialized();

        var unique = new Dictionary<string, NicsellDomainRecord>(StringComparer.OrdinalIgnoreCase);
        var visitedPages = 0;
        var page = 1;
        NicsellPaginationInfo? pagination = null;

        while (true)
        {
            if (_options.MaxPages is { } maxPages && page > maxPages)
            {
                Console.WriteLine($"Остановка по MaxPages={maxPages} (не все страницы выгружены).");
                break;
            }

            var pageUrl = BuildPageUrl(filter, page);
            Console.WriteLine($"Запрос страницы {page.ToString()}: {pageUrl}");
            var html = await GetHtmlAsync(pageUrl, cancellationToken);
            if (page == 1)
            {
                pagination = ParsePaginationFromHtml(html, _options.MaxPerPage);
                if (pagination!.TotalPages is { } totalP)
                {
                    Console.WriteLine(
                        $"Пагинация: текущая={pagination.CurrentPage}, всего страниц={totalP}, записей на странице={pagination.EntriesPerPageFromHtml}, примерно записей={pagination.ApproximateTotalEntries}");
                }
                else
                {
                    Console.WriteLine("Не удалось определить TotalPages из HTML; обход остановится на пустой странице или по MaxPages.");
                }
            }

            var pageItems = ParseDomains(html);
            visitedPages++;

            if (pageItems.Count == 0)
            {
                Console.WriteLine($"Страница {page}: записей 0 — конец выдачи.");
                break;
            }

            Console.WriteLine($"Страница {page}: на странице {pageItems.Count} доменов.");

            foreach (var item in pageItems)
            {
                unique[item.Domain] = item;
            }

            Console.WriteLine($"Всего уникальных доменов после страницы {page}: {unique.Count}.");

            if (pagination?.TotalPages is { } lastPage && page >= lastPage)
            {
                Console.WriteLine($"Достигнута последняя страница пагинации ({lastPage}).");
                break;
            }

            page++;

            if (_options.RequestDelay > TimeSpan.Zero)
            {
                await Task.Delay(_options.RequestDelay, cancellationToken);
            }
        }

        Console.WriteLine(
            $"Парсинг завершён: обойдено страниц {visitedPages}, уникальных доменов {unique.Count}.");

        return new NicsellParseResult
        {
            Items = unique.Values.OrderBy(x => x.Domain, StringComparer.OrdinalIgnoreCase).ToArray(),
            VisitedPages = visitedPages,
            ParsedAtUtc = DateTimeOffset.UtcNow,
            Pagination = pagination
        };
    }

    /// <summary>
    /// Извлекает пагинацию из HTML ответа списка доменов (десктопный <c>ul.pagination</c>).
    /// Последняя страница — максимальный <c>page=</c> в ссылках (в т.ч. «»|»).
    /// </summary>
    public static NicsellPaginationInfo ParsePaginationFromHtml(string html, int? entriesPerPageFallback = null)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        int? entriesPerPage = null;
        var maxPerPageBlock = doc.DocumentNode.SelectSingleNode("//div[contains(concat(' ',normalize-space(@class),' '),' max-per-page ')]");
        if (maxPerPageBlock is not null)
        {
            var selected = maxPerPageBlock.SelectSingleNode(".//a[contains(@href,'maxperpage=') and contains(@style,'underline')]")
                ?? maxPerPageBlock.SelectSingleNode(".//a[contains(@href,'maxperpage=') and contains(@class,'color-darkgray')]");
            var href = WebUtility.HtmlDecode(selected?.GetAttributeValue("href", string.Empty) ?? string.Empty);
            var mpp = MaxPerPageQueryRegex.Match(href);
            if (mpp.Success && int.TryParse(mpp.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mppVal))
            {
                entriesPerPage = mppVal;
            }
        }

        var paginationRoot = doc.DocumentNode.SelectSingleNode(
            "//ul[contains(concat(' ',normalize-space(@class),' '),' pagination ') and not(contains(@class,'pagination-navigation-mobile'))]");

        int? currentPage = null;
        var activeLink = paginationRoot?.SelectSingleNode(".//li[contains(@class,'active')]//a[@href]");
        if (activeLink is not null)
        {
            var label = CleanText(activeLink.InnerText);
            if (int.TryParse(label, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cp))
            {
                currentPage = cp;
            }
            else
            {
                var href = WebUtility.HtmlDecode(activeLink.GetAttributeValue("href", string.Empty));
                var pm = PageQueryRegex.Match(href);
                if (pm.Success && int.TryParse(pm.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fromHref))
                {
                    currentPage = fromHref;
                }
            }
        }

        int? totalPages = null;
        if (paginationRoot is not null)
        {
            var pageNumbers = new List<int>();
            foreach (var a in paginationRoot.SelectNodes(".//a[@href]")?.AsEnumerable() ?? Enumerable.Empty<HtmlNode>())
            {
                var href = WebUtility.HtmlDecode(a.GetAttributeValue("href", string.Empty));
                if (!href.Contains("domainlist", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (Match match in PageQueryRegex.Matches(href))
                {
                    if (int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var p))
                    {
                        pageNumbers.Add(p);
                    }
                }
            }

            if (pageNumbers.Count > 0)
            {
                totalPages = pageNumbers.Max();
            }
        }

        var epp = entriesPerPage ?? entriesPerPageFallback;
        int? approx = null;
        if (totalPages is > 0 && epp is > 0)
        {
            approx = totalPages.Value * epp.Value;
        }

        return new NicsellPaginationInfo
        {
            CurrentPage = currentPage,
            TotalPages = totalPages,
            EntriesPerPageFromHtml = entriesPerPage,
            ApproximateTotalEntries = approx
        };
    }

    private void EnsureClientInitialized()
    {
        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(_options.BaseAddress);
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("Cookie"))
        {
            _httpClient.DefaultRequestHeaders.Add("Cookie", _options.CookieHeader);
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; NicsellParser/1.0)");
        }
    }

    private async Task<string> GetHtmlAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        Console.WriteLine($"HTTP GET начало: {relativeUrl}");
        using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);
        var status = (int)response.StatusCode;
        if (!response.IsSuccessStatusCode)
        {
            sw.Stop();
            Console.WriteLine(
                $"HTTP GET {relativeUrl} завершился с кодом {status} за {sw.ElapsedMilliseconds} мс.");
            response.EnsureSuccessStatusCode();
        }

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        sw.Stop();
        Console.WriteLine(
            $"HTTP GET {relativeUrl} -> {status}, {html.Length} байт HTML, {sw.ElapsedMilliseconds} мс.");

        if (html.Contains("In order to see the domainlist you have to be logged in.", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Ответ похож на страницу входа (нет доступа к domainlist).");
            throw new InvalidOperationException(
                "Nicsell вернул страницу логина. Проверьте корректность CookieHeader в конфигурации парсера.");
        }

        return html;
    }

    private string BuildPageUrl(NicsellFilter filter, int page)
    {
        var query = new List<KeyValuePair<string, string>>
        {
            new("mode", filter.Mode),
            new("maxperpage", _options.MaxPerPage.ToString(CultureInfo.InvariantCulture)),
            new("page", page.ToString(CultureInfo.InvariantCulture))
        };

        AddIfValue(query, "q", filter.Query);
        AddIfValue(query, "lengthfrom", filter.LengthFrom);
        AddIfValue(query, "lengthto", filter.LengthTo);
        AddIfValue(query, "datefrom", filter.DateFrom);
        AddIfValue(query, "dateto", filter.DateTo);
        AddIfValue(query, "option", filter.Option);
        AddIfValue(query, "sort", filter.Sort);

        foreach (var tld in filter.Tlds.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var normalized = tld.Trim().TrimStart('.').ToLowerInvariant();
            query.Add(new KeyValuePair<string, string>($"onlyTld[{normalized}]", "1"));
        }

        foreach (var lang in filter.NoDictionaryLanguages.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            query.Add(new KeyValuePair<string, string>($"nodict[{lang.Trim().ToLowerInvariant()}]", "1"));
        }

        var sb = new StringBuilder($"/{_options.CulturePath.Trim('/')}/domainlist?");
        for (var i = 0; i < query.Count; i++)
        {
            var pair = query[i];
            if (i > 0)
            {
                sb.Append('&');
            }

            sb.Append(Uri.EscapeDataString(pair.Key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(pair.Value));
        }

        return sb.ToString();
    }

    private static void AddIfValue(List<KeyValuePair<string, string>> query, string key, object? value)
    {
        if (value is null)
        {
            return;
        }

        string formatted = value switch
        {
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(formatted))
        {
            query.Add(new KeyValuePair<string, string>(key, formatted));
        }
    }

    private static List<NicsellDomainRecord> ParseDomains(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var rows = doc.DocumentNode.SelectNodes("//div[@id='domainliste']//div[contains(@class,'row') and @data-domain-idn]")
            ?.AsEnumerable() ?? Enumerable.Empty<HtmlNode>();
        var result = new List<NicsellDomainRecord>();

        foreach (var row in rows)
        {
            var domain = row.GetAttributeValue("data-domain-idn", string.Empty)?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(domain) || !DomainRegex.IsMatch(domain))
            {
                continue;
            }

            var domainId = TryParseLong(row.GetAttributeValue("data-domain-id", string.Empty));
            var rowText = CleanText(row.InnerText);
            var tld = domain[(domain.LastIndexOf('.') + 1)..];
            var detailsUrl = domainId is long id
                ? $"https://nicsell.com/en/domain/{id}"
                : null;

            var bidNode = row.SelectSingleNode(".//span[contains(@class,'amount-current-bid')]");
            var currentBid = TryParseDecimal(bidNode?.GetAttributeValue("data-current-bid", string.Empty));

            var auctionNode = row.SelectSingleNode(".//span[contains(@class,'liveauctiontimer')]");
            var auctionEndTimestamp = TryParseLong(auctionNode?.GetAttributeValue("data-auctionend-timestamp", string.Empty));
            var auctionEnd = auctionEndTimestamp.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(auctionEndTimestamp.Value)
                : (DateTimeOffset?)null;

            var auctionDisplay = auctionNode?.GetAttributeValue("data-original-title", string.Empty);
            auctionDisplay = string.IsNullOrWhiteSpace(auctionDisplay) ? null : WebUtility.HtmlDecode(auctionDisplay);

            var bidCountNode = row.SelectSingleNode(".//span[contains(@class,'count-bids')]");
            var bidCount = TryParseInt(bidCountNode?.GetAttributeValue("data-count-bids", string.Empty));

            var colValues = row.SelectNodes("./div[contains(concat(' ', normalize-space(@class), ' '), ' col-value ')]")
                ?.AsEnumerable()
                .ToArray() ?? [];

            // Колонки после Count bids идут в фиксированном порядке (inTld, RD, BL, TF, CF, TF/CF, Google, Dictionary, ARC, Chars)
            int? GetIntByIndex(int index) => colValues.Length > index ? TryParseInt(CleanText(colValues[index].InnerText)) : null;
            decimal? GetDecimalByIndex(int index) => colValues.Length > index ? TryParseDecimal(CleanText(colValues[index].InnerText)) : null;

            var isQuarantine = row.SelectSingleNode(".//span[contains(@title,'Quarantine domain')]") is not null;
            var isPremium = row.SelectSingleNode(".//span[contains(@title,'Premium')]") is not null;

            if (string.IsNullOrWhiteSpace(rowText))
            {
                continue;
            }

            result.Add(new NicsellDomainRecord
            {
                Domain = domain,
                DomainId = domainId,
                Tld = tld,
                CurrentBidEur = currentBid,
                BidCount = bidCount,
                AuctionEndsAt = auctionEnd,
                AuctionEndsDisplay = auctionDisplay,
                InTldCount = GetIntByIndex(3),
                MajesticRefDomains = GetIntByIndex(4),
                MajesticBacklinks = GetIntByIndex(5),
                MajesticTrustFlow = GetIntByIndex(6),
                MajesticCitationFlow = GetIntByIndex(7),
                MajesticTfCfRatio = GetDecimalByIndex(8),
                GoogleHits = GetIntByIndex(9),
                ArchiveOrgResults = GetIntByIndex(11),
                NameLength = GetIntByIndex(12),
                IsQuarantine = isQuarantine,
                IsPremium = isPremium,
                DetailsUrl = detailsUrl
            });
        }

        return result;
    }

    private static int? TryParseInt(string? source)
    {
        if (string.IsNullOrWhiteSpace(source) || source.Trim() == "-")
        {
            return null;
        }

        var normalized = Regex.Replace(source, @"[^\d\-]", string.Empty);
        return int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static long? TryParseLong(string? source)
    {
        if (string.IsNullOrWhiteSpace(source) || source.Trim() == "-")
        {
            return null;
        }

        var normalized = Regex.Replace(source, @"[^\d\-]", string.Empty);
        return long.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static decimal? TryParseDecimal(string? source)
    {
        if (string.IsNullOrWhiteSpace(source) || source.Trim() == "-")
        {
            return null;
        }

        var normalized = source
            .Replace("€", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("eur", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        // Если есть только запятая, трактуем как десятичный разделитель, иначе удаляем разделители тысяч.
        if (normalized.Contains(',', StringComparison.Ordinal) && !normalized.Contains('.', StringComparison.Ordinal))
        {
            normalized = normalized.Replace(",", ".", StringComparison.Ordinal);
        }
        else
        {
            normalized = normalized.Replace(",", string.Empty, StringComparison.Ordinal);
        }

        normalized = normalized.Replace(" ", string.Empty, StringComparison.Ordinal);

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static string CleanText(string? value)
    {
        return Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
    }

}
