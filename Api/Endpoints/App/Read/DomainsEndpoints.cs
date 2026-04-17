using Application.Services.Read;
using Api.Endpoints.App;
using FastEndpoints;
using Infrastructure.Read;

namespace Api.Endpoints.App.Read;

public sealed class GetDomainsRequest
{
    public string? Search { get; set; }
    public string? Tld { get; set; }
    public string? Sort { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class GetDomainByIdRequest
{
    public long Id { get; set; }
}

public sealed class GetDomainsEndpoint : Endpoint<GetDomainsRequest, BaseResponse<List<DomainListItem>>>
{
    private readonly IReceiverReadService _readService;

    public GetDomainsEndpoint(IReceiverReadService readService)
    {
        _readService = readService;
    }

    public override void Configure()
    {
        Get("read/domains");
        Group<AppGroupEndpoints>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetDomainsRequest req, CancellationToken ct)
    {
        var result = await _readService.GetDomainsAsync(new DomainListQuery
        {
            Search = req.Search,
            Tld = req.Tld,
            Sort = req.Sort,
            PageNumber = req.PageNumber,
            PageSize = req.PageSize
        }, ct);

        await SendAsync(BaseResponse<List<DomainListItem>>.Ok(result.Data, meta: new
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

public sealed class GetDomainByIdEndpoint : Endpoint<GetDomainByIdRequest, BaseResponse<DomainDetailsItem>>
{
    private readonly IReceiverReadService _readService;

    public GetDomainByIdEndpoint(IReceiverReadService readService)
    {
        _readService = readService;
    }

    public override void Configure()
    {
        Get("read/domains/{id:long}");
        Group<AppGroupEndpoints>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetDomainByIdRequest req, CancellationToken ct)
    {
        var domain = await _readService.GetDomainAsync(req.Id, ct);
        if (domain is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendAsync(BaseResponse<DomainDetailsItem>.Ok(domain), cancellation: ct);
    }
}
