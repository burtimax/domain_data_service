using FastEndpoints;
using Infrastructure.Db.App;
using Microsoft.EntityFrameworkCore;
using Api.Endpoints.App;

namespace Api.Endpoints.App.HealthCheck;

class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset UtcNow { get; set; }
}

sealed class ReadinessResponse : HealthCheckResponse
{
    public bool DatabaseReady { get; set; }
}

sealed class HealthCheckEndpoint : EndpointWithoutRequest<BaseResponse<HealthCheckResponse>>
{
    public override void Configure()
    {
        Get("health");
        AllowAnonymous();
        Group<AppGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Проверка состояния приложения";
            s.Description = "Liveness endpoint: процесс API запущен и отвечает.";
        });
    }

    public override async Task HandleAsync(CancellationToken c)
    {
        var response = new HealthCheckResponse
        {
            Status = "healthy",
            UtcNow = DateTimeOffset.UtcNow
        };

        await SendAsync(BaseResponse<HealthCheckResponse>.Ok(response), cancellation: c);
    }
}

sealed class ReadinessEndpoint : EndpointWithoutRequest<BaseResponse<ReadinessResponse>>
{
    private readonly AppDbContext _dbContext;

    public ReadinessEndpoint(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("readiness");
        AllowAnonymous();
        Group<AppGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Проверка готовности зависимостей";
            s.Description = "Readiness endpoint: проверяет доступность БД для read API.";
        });
    }

    public override async Task HandleAsync(CancellationToken c)
    {
        var dbReady = await _dbContext.Database.CanConnectAsync(c);
        var payload = new ReadinessResponse
        {
            Status = dbReady ? "ready" : "not_ready",
            UtcNow = DateTimeOffset.UtcNow,
            DatabaseReady = dbReady
        };

        if (!dbReady)
        {
            await SendAsync(BaseResponse<ReadinessResponse>.Fail("База данных недоступна"), 503, c);
            return;
        }

        await SendAsync(BaseResponse<ReadinessResponse>.Ok(payload), cancellation: c);
    }
}