using Microsoft.EntityFrameworkCore;
using Lopingo.Data;
using Incident = Lopingo.Data.Entities.Incident;

namespace Lopingo.Repositories;

public sealed class IncidentRepository
{
    private readonly AppDbContext _db;

    public IncidentRepository(AppDbContext db) => _db = db;

    public async Task<Incident?> GetOpenByMonitorAsync(Guid monitorId, CancellationToken ct = default)
        => await _db.Incidents.Where(i => i.MonitorId == monitorId && i.EndedAt == null)
            .OrderByDescending(i => i.StartedAt).FirstOrDefaultAsync(ct);

    public Incident Open(Guid monitorId, string? firstError, DateTime now)
    {
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            MonitorId = monitorId,
            StartedAt = now,
            FirstError = firstError,
            LastError = firstError,
            CreatedAt = now,
        };
        _db.Incidents.Add(incident);
        return incident;
    }

    public void Close(Incident open, DateTime now, string? lastError)
    {
        open.EndedAt = now;
        open.DurationSec = (long)(now - open.StartedAt).TotalSeconds;
        open.LastError = lastError;
    }

    public async Task<List<Incident>> ListByMonitorAsync(Guid monitorId, int limit, CancellationToken ct = default)
        => await _db.Incidents.Where(i => i.MonitorId == monitorId)
            .OrderByDescending(i => i.StartedAt).Take(limit).ToListAsync(ct);
}
