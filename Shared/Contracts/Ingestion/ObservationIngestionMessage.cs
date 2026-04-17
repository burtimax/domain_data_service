namespace Shared.Contracts.Ingestion;

public sealed class ObservationIngestionMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = string.Empty;
    public string ParserCode { get; set; } = string.Empty;
    public string ParserVersion { get; set; } = string.Empty;
    public string SourcePlatformCode { get; set; } = string.Empty;
    public DateTimeOffset ObservedAt { get; set; }
    public string ExternalAuctionId { get; set; } = string.Empty;
    public string? ExternalDomainId { get; set; }
    public string DomainNameOriginal { get; set; } = string.Empty;
    public string? DomainNameNormalized { get; set; }
    public string AuctionStatusRaw { get; set; } = string.Empty;
    public string? AuctionStatusNormalized { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTimeOffset? AuctionStartAt { get; set; }
    public DateTimeOffset? AuctionEndAt { get; set; }
    public DateTimeOffset? AuctionExtendedEndAt { get; set; }
    public bool? IsExtended { get; set; }
    public string? LotUrl { get; set; }
    public string? AttributesJson { get; set; }
    public string? RawPayload { get; set; }
}
