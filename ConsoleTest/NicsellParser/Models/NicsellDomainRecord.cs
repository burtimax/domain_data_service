namespace ConsoleTest.NicsellParser.Models;

public sealed class NicsellDomainRecord
{
    public required string Domain { get; init; }
    public long? DomainId { get; init; }
    public string? Tld { get; init; }
    public decimal? CurrentBidEur { get; init; }
    public int? BidCount { get; init; }
    public DateTimeOffset? AuctionEndsAt { get; init; }
    public string? AuctionEndsDisplay { get; init; }
    public int? InTldCount { get; init; }
    public int? MajesticRefDomains { get; init; }
    public int? MajesticBacklinks { get; init; }
    public int? MajesticTrustFlow { get; init; }
    public int? MajesticCitationFlow { get; init; }
    public decimal? MajesticTfCfRatio { get; init; }
    public int? GoogleHits { get; init; }
    public int? ArchiveOrgResults { get; init; }
    public int? NameLength { get; init; }
    public bool IsQuarantine { get; init; }
    public bool IsPremium { get; init; }
    public string? DetailsUrl { get; init; }
}
