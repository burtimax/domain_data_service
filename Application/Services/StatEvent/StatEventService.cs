using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;

namespace Application.Services.StatEvent;

public class StatEventService : IStatEventService
{
    private readonly AppDbContext _db;

    public StatEventService(AppDbContext db)
    {
        _db = db;
    }


    public async Task<StatEventEntity> CreateStatEventAsync(string? type, CancellationToken ct = default)
    {
        var statEvent = new StatEventEntity
        {
            Type = (type ?? "").Substring(0, Math.Min(type?.Length ?? 0, 30))
        };

        await _db.StatEvents.AddAsync(statEvent, ct);
        await _db.SaveChangesAsync(ct);
        return statEvent;
    }
}
