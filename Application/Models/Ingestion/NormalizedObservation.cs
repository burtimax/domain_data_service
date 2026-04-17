namespace Application.Models.Ingestion;

public sealed class NormalizedObservation
{
    public string MessageId { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = string.Empty;
    public string ParserCode { get; set; } = string.Empty;
    public string ParserVersion { get; set; } = string.Empty;
    public string SourcePlatformCode { get; set; } = string.Empty;
    public DateTimeOffset ObservedAtUtc { get; set; }
    public string ExternalAuctionId { get; set; } = string.Empty;
    public string DomainNameOriginal { get; set; } = string.Empty;
    public string DomainNameNormalized { get; set; } = string.Empty;
    public string DomainNamePunycode { get; set; } = string.Empty;
    public string Tld { get; set; } = string.Empty;
    public string Sld { get; set; } = string.Empty;
    public string AuctionStatusRaw { get; set; } = string.Empty;
    public string AuctionStatusNormalized { get; set; } = "unknown";
    public decimal? CurrentPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTimeOffset? AuctionStartAtUtc { get; set; }
    public DateTimeOffset? AuctionEndAtUtc { get; set; }
    public DateTimeOffset? AuctionExtendedEndAtUtc { get; set; }
    public bool? IsExtended { get; set; }
    public string? LotUrl { get; set; }
    public string? AttributesJson { get; set; }
    public string? RawPayload { get; set; }

    public string SemanticFingerprint { get; set; } = string.Empty;
    public bool IsTerminal => TerminalStatuses.Contains(AuctionStatusNormalized);

    private static readonly HashSet<string> TerminalStatuses =
        ["sold", "not_sold", "cancelled", "disappeared"];
}
