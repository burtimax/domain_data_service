using Application.Services.Read;
using Api.Endpoints.App;
using FastEndpoints;
using Infrastructure.Read;

namespace Api.Endpoints.App.Read;

public sealed class GetAuctionsRequest
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

public sealed class GetAuctionByIdRequest
{
    public long Id { get; set; }
}

public sealed class GetAuctionObservationsRequest
{
    public long Id { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class GetAuctionStatusHistoryRequest
{
    public long Id { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class GetAuctionsEndpoint : Endpoint<GetAuctionsRequest, BaseResponse<List<AuctionListItem>>>
{
    private readonly IReceiverReadService _readService;

    public GetAuctionsEndpoint(IReceiverReadService readService)
    {
        _readService = readService;
    }

    public override void Configure()
    {
        Get("read/auctions");
        Group<AppGroupEndpoints>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetAuctionsRequest req, CancellationToken ct)
    {
        var result = await _readService.GetAuctionsAsync(new AuctionListQuery
        {
            DomainId = req.DomainId,
            DomainName = req.DomainName,
            SourceCode = req.SourceCode,
            Status = req.Status,
            TerminalOnly = req.TerminalOnly,
            Sort = req.Sort,
            PageNumber = req.PageNumber,
            PageSize = req.PageSize
        }, ct);

        await SendAsync(BaseResponse<List<AuctionListItem>>.Ok(result.Data, meta: new
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

public sealed class GetAuctionByIdEndpoint : Endpoint<GetAuctionByIdRequest, BaseResponse<AuctionDetailsItem>>
{
    private readonly IReceiverReadService _readService;

    public GetAuctionByIdEndpoint(IReceiverReadService readService)
    {
        _readService = readService;
    }

    public override void Configure()
    {
        Get("read/auctions/{id:long}");
        Group<AppGroupEndpoints>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetAuctionByIdRequest req, CancellationToken ct)
    {
        var auction = await _readService.GetAuctionAsync(req.Id, ct);
        if (auction is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendAsync(BaseResponse<AuctionDetailsItem>.Ok(auction), cancellation: ct);
    }
}

public sealed class GetAuctionObservationsEndpoint : Endpoint<GetAuctionObservationsRequest, BaseResponse<List<AuctionObservationItem>>>
{
    private readonly IReceiverReadService _readService;

    public GetAuctionObservationsEndpoint(IReceiverReadService readService)
    {
        _readService = readService;
    }

    public override void Configure()
    {
        Get("read/auctions/{id:long}/observations");
        Group<AppGroupEndpoints>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetAuctionObservationsRequest req, CancellationToken ct)
    {
        var result = await _readService.GetAuctionObservationsAsync(req.Id, req.PageNumber, req.PageSize, ct);
        await SendAsync(BaseResponse<List<AuctionObservationItem>>.Ok(result.Data, meta: new
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

public sealed class GetAuctionStatusHistoryEndpoint : Endpoint<GetAuctionStatusHistoryRequest, BaseResponse<List<AuctionStatusHistoryItem>>>
{
    private readonly IReceiverReadService _readService;

    public GetAuctionStatusHistoryEndpoint(IReceiverReadService readService)
    {
        _readService = readService;
    }

    public override void Configure()
    {
        Get("read/auctions/{id:long}/status-history");
        Group<AppGroupEndpoints>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetAuctionStatusHistoryRequest req, CancellationToken ct)
    {
        var result = await _readService.GetAuctionStatusHistoryAsync(req.Id, req.PageNumber, req.PageSize, ct);
        await SendAsync(BaseResponse<List<AuctionStatusHistoryItem>>.Ok(result.Data, meta: new
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
