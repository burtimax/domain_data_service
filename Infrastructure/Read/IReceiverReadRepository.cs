using Infrastructure.Models;

namespace Infrastructure.Read;

public interface IReceiverReadRepository
{
    Task<PagedList<DomainListItem>> GetDomainsAsync(DomainListQuery query, CancellationToken ct);
    Task<DomainDetailsItem?> GetDomainByIdAsync(long domainId, CancellationToken ct);
    Task<PagedList<AuctionListItem>> GetAuctionsAsync(AuctionListQuery query, CancellationToken ct);
    Task<AuctionDetailsItem?> GetAuctionByIdAsync(long auctionId, CancellationToken ct);
    Task<PagedList<AuctionObservationItem>> GetAuctionObservationsAsync(long auctionId, int pageNumber, int pageSize, CancellationToken ct);
    Task<PagedList<AuctionStatusHistoryItem>> GetAuctionStatusHistoryAsync(long auctionId, int pageNumber, int pageSize, CancellationToken ct);
    Task<PagedList<AuctionResultItem>> GetAuctionResultsAsync(AuctionResultsQuery query, CancellationToken ct);
}

public sealed class DomainListQuery
{
    public string? Search { get; set; }
    public string? Tld { get; set; }
    public string? Sort { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class AuctionListQuery
{
    public long? DomainId { get; set; }
    public string? DomainName { get; set; }
    public string? SourceCode { get; set; }
    public string? Status { get; set; }
    public bool? TerminalOnly { get; set; }
    public string? Sort { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class AuctionResultsQuery
{
    public long? DomainId { get; set; }
    public string? SourceCode { get; set; }
    public string? TerminalStatus { get; set; }
    public string? Sort { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class DomainListItem
{
    public long Id { get; set; }
    public string NameOriginal { get; set; } = string.Empty;
    public string NameNormalized { get; set; } = string.Empty;
    public string NamePunycode { get; set; } = string.Empty;
    public string Tld { get; set; } = string.Empty;
    public string Sld { get; set; } = string.Empty;
    public int AuctionsCount { get; set; }
}

public sealed class DomainDetailsItem : DomainListItem
{
    public DateTimeOffset CreatedAt { get; set; }
}

public class AuctionListItem
{
    public long Id { get; set; }
    public long DomainId { get; set; }
    public string DomainName { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string ExternalAuctionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? CurrentPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string? TerminalStatus { get; set; }
    public DateTimeOffset? FinalizedAt { get; set; }
    public DateTimeOffset? LastObservedAt { get; set; }
}

public sealed class AuctionDetailsItem : AuctionListItem
{
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? ExtendedEndAt { get; set; }
    public bool? IsExtended { get; set; }
    public string? LotUrl { get; set; }
}

public sealed class AuctionObservationItem
{
    public long Id { get; set; }
    public long AuctionId { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = string.Empty;
    public string ParserCode { get; set; } = string.Empty;
    public string ParserVersion { get; set; } = string.Empty;
    public DateTimeOffset ObservedAt { get; set; }
    public string AuctionStatusRaw { get; set; } = string.Empty;
    public string AuctionStatusNormalized { get; set; } = string.Empty;
    public decimal? CurrentPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public bool IsOutOfOrder { get; set; }
}

public sealed class AuctionStatusHistoryItem
{
    public long Id { get; set; }
    public long AuctionId { get; set; }
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public bool IsTerminal { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public string Source { get; set; } = string.Empty;
}

public sealed class AuctionResultItem
{
    public long AuctionId { get; set; }
    public long DomainId { get; set; }
    public string DomainName { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public string ExternalAuctionId { get; set; } = string.Empty;
    public string TerminalStatus { get; set; } = string.Empty;
    public decimal? FinalPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTimeOffset? FinalizedAt { get; set; }
}
