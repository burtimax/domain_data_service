using Application.Services.Read;
using Api.Endpoints.App;
using FastEndpoints;
using Infrastructure.Read;

namespace Api.Endpoints.App.Read;

public sealed class GetAuctionResultsRequest
{
    public long? DomainId { get; set; }
    public string? SourceCode { get; set; }
    public string? TerminalStatus { get; set; }
    public string? Sort { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class GetAuctionResultsEndpoint : Endpoint<GetAuctionResultsRequest, BaseResponse<List<AuctionResultItem>>>
{
    private readonly IReceiverReadService _readService;

    public GetAuctionResultsEndpoint(IReceiverReadService readService)
    {
        _readService = readService;
    }

    public override void Configure()
    {
        Get("read/results");
        Group<AppGroupEndpoints>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetAuctionResultsRequest req, CancellationToken ct)
    {
        var result = await _readService.GetAuctionResultsAsync(new AuctionResultsQuery
        {
            DomainId = req.DomainId,
            SourceCode = req.SourceCode,
            TerminalStatus = req.TerminalStatus,
            Sort = req.Sort,
            PageNumber = req.PageNumber,
            PageSize = req.PageSize
        }, ct);

        await SendAsync(BaseResponse<List<AuctionResultItem>>.Ok(result.Data, meta: new
        {
            result.CurrentPage,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            result.HasPrevious,
            result.HasNext
        }), cancellation: ct);
    }
}
