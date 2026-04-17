using Infrastructure.Db.App.Entities;

namespace Application.Services.StatEvent;

public interface IStatEventService
{
    Task<StatEventEntity> CreateStatEventAsync(string? type, CancellationToken ct = default);
}
