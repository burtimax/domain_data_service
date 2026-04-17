using Application.Services.StatEvent;
using FastEndpoints;
using Infrastructure.Db.App.Entities;
using Shared.Contracts;

namespace Api.Endpoints.App.SaveStatEvent;

/// <summary>
/// Endpoint для сохранения статистического события
/// </summary>
sealed class SaveStatEventEndpoint : Endpoint<SaveStatEventRequest, Result<StatEventEntity>>
{
    private readonly IStatEventService _statEventService;

    public SaveStatEventEndpoint(IStatEventService statEventService)
    {
        _statEventService = statEventService;
    }

    public override void Configure()
    {
        Post("stat-event");
        Group<AppGroupEndpoints>();
        Summary(s =>
        {
            s.Summary = "Сохранение события статистики";
            s.Description = "Сохраняет событие использования приложения для аналитики";
        });
    }

    public override async Task HandleAsync(SaveStatEventRequest req, CancellationToken ct)
    {
        var statEvent = await _statEventService.CreateStatEventAsync(req.Type, ct);

        await SendAsync(new Result<StatEventEntity>(statEvent), cancellation: ct);
    }
}
