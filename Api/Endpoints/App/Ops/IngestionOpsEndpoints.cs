using Api.Endpoints.App;
using Api.Services;
using Application.Services.Ingestion;
using FastEndpoints;
using Shared.Configs;

namespace Api.Endpoints.App.Ops;

public sealed class ReplayRequest
{
    public int? MaxMessages { get; set; }
}

public sealed class IngestionStatusResponse
{
    public IngestionMetricsSnapshot Metrics { get; set; } = null!;
    public QueueStatusSnapshot Queue { get; set; } = null!;
}

public sealed class ReplayResponse
{
    public string Source { get; set; } = string.Empty;
    public ReplayResult Result { get; set; } = null!;
}

public sealed class GetIngestionStatusEndpoint : EndpointWithoutRequest<BaseResponse<IngestionStatusResponse>>
{
    private readonly IngestionMetrics _metrics;
    private readonly IRabbitMqOpsService _opsService;

    public GetIngestionStatusEndpoint(IngestionMetrics metrics, IRabbitMqOpsService opsService)
    {
        _metrics = metrics;
        _opsService = opsService;
    }

    public override void Configure()
    {
        Get("ops/ingestion-status");
        Group<AppGroupEndpoints>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var response = new IngestionStatusResponse
        {
            Metrics = _metrics.Snapshot(),
            Queue = await _opsService.GetQueueStatusAsync(ct)
        };
        await SendAsync(BaseResponse<IngestionStatusResponse>.Ok(response), cancellation: ct);
    }
}

public sealed class ReplayDlqEndpoint : Endpoint<ReplayRequest, BaseResponse<ReplayResponse>>
{
    private readonly IRabbitMqOpsService _opsService;
    private readonly AppConfiguration _config;

    public ReplayDlqEndpoint(IRabbitMqOpsService opsService, AppConfiguration config)
    {
        _opsService = opsService;
        _config = config;
    }

    public override void Configure()
    {
        Post("ops/replay/dlq");
        Group<AppGroupEndpoints>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(ReplayRequest req, CancellationToken ct)
    {
        var max = ResolveBatchSize(req.MaxMessages);
        var result = await _opsService.ReplayFromDlqAsync(max, ct);
        await SendAsync(BaseResponse<ReplayResponse>.Ok(new ReplayResponse
        {
            Source = "dlq",
            Result = result
        }), cancellation: ct);
    }

    private int ResolveBatchSize(int? requested)
    {
        var max = _config.RabbitMq.ReplayBatchSizeMax;
        var value = requested ?? _config.RabbitMq.ReplayBatchSizeDefault;
        return Math.Clamp(value, 1, max);
    }
}

public sealed class ReplayArchiveEndpoint : Endpoint<ReplayRequest, BaseResponse<ReplayResponse>>
{
    private readonly IRabbitMqOpsService _opsService;
    private readonly AppConfiguration _config;

    public ReplayArchiveEndpoint(IRabbitMqOpsService opsService, AppConfiguration config)
    {
        _opsService = opsService;
        _config = config;
    }

    public override void Configure()
    {
        Post("ops/replay/archive");
        Group<AppGroupEndpoints>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(ReplayRequest req, CancellationToken ct)
    {
        var max = ResolveBatchSize(req.MaxMessages);
        var result = await _opsService.ReplayFromArchiveAsync(max, ct);
        await SendAsync(BaseResponse<ReplayResponse>.Ok(new ReplayResponse
        {
            Source = "archive",
            Result = result
        }), cancellation: ct);
    }

    private int ResolveBatchSize(int? requested)
    {
        var max = _config.RabbitMq.ReplayBatchSizeMax;
        var value = requested ?? _config.RabbitMq.ReplayBatchSizeDefault;
        return Math.Clamp(value, 1, max);
    }
}
