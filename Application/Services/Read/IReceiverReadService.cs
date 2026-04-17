using Infrastructure.Models;
using Infrastructure.Read;

namespace Application.Services.Read;

public interface IReceiverReadService
{
    Task<PagedList<DomainListItem>> GetDomainsAsync(DomainListQuery query, CancellationToken ct);
    Task<DomainDetailsItem?> GetDomainAsync(long domainId, CancellationToken ct);
    Task<PagedList<AuctionListItem>> GetAuctionsAsync(AuctionListQuery query, CancellationToken ct);
    Task<AuctionDetailsItem?> GetAuctionAsync(long auctionId, CancellationToken ct);
    Task<PagedList<AuctionObservationItem>> GetAuctionObservationsAsync(long auctionId, int pageNumber, int pageSize, CancellationToken ct);
    Task<PagedList<AuctionStatusHistoryItem>> GetAuctionStatusHistoryAsync(long auctionId, int pageNumber, int pageSize, CancellationToken ct);
    Task<PagedList<AuctionResultItem>> GetAuctionResultsAsync(AuctionResultsQuery query, CancellationToken ct);
}
