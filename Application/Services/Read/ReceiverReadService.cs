using Infrastructure.Models;
using Infrastructure.Read;

namespace Application.Services.Read;

public sealed class ReceiverReadService : IReceiverReadService
{
    private readonly IReceiverReadRepository _repository;

    public ReceiverReadService(IReceiverReadRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedList<DomainListItem>> GetDomainsAsync(DomainListQuery query, CancellationToken ct)
        => _repository.GetDomainsAsync(query, ct);

    public Task<DomainDetailsItem?> GetDomainAsync(long domainId, CancellationToken ct)
        => _repository.GetDomainByIdAsync(domainId, ct);

    public Task<PagedList<AuctionListItem>> GetAuctionsAsync(AuctionListQuery query, CancellationToken ct)
        => _repository.GetAuctionsAsync(query, ct);

    public Task<AuctionDetailsItem?> GetAuctionAsync(long auctionId, CancellationToken ct)
        => _repository.GetAuctionByIdAsync(auctionId, ct);

    public Task<PagedList<AuctionObservationItem>> GetAuctionObservationsAsync(long auctionId, int pageNumber, int pageSize, CancellationToken ct)
        => _repository.GetAuctionObservationsAsync(auctionId, pageNumber, pageSize, ct);

    public Task<PagedList<AuctionStatusHistoryItem>> GetAuctionStatusHistoryAsync(long auctionId, int pageNumber, int pageSize, CancellationToken ct)
        => _repository.GetAuctionStatusHistoryAsync(auctionId, pageNumber, pageSize, ct);

    public Task<PagedList<AuctionResultItem>> GetAuctionResultsAsync(AuctionResultsQuery query, CancellationToken ct)
        => _repository.GetAuctionResultsAsync(query, ct);
}
