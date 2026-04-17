using Infrastructure.Db.App.Entities;

namespace Application.Services.StatEvent;

public interface IStatEventService
{
    Task<StatEventEntity> CreateStatEventAsync(long userId, long sessionId, string? utm, string? type, CancellationToken ct = default);
}
