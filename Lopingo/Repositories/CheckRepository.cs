using Microsoft.EntityFrameworkCore;
using Lopingo.Core.Engine;
using Lopingo.Data;
using Check = Lopingo.Data.Entities.Check;

namespace Lopingo.Repositories;

public sealed record MonitorPeriodStats(
    int ResultChecks,
    int UpChecks,
    double? UptimePercent,
    double? AvgResponseMs,
    DateTime? OldestCheckedAt,
    DateTime? NewestCheckedAt);

public sealed class CheckRepository
{
    private readonly AppDbContext _db;

    public CheckRepository(AppDbContext db) => _db = db;

    public void Add(Check check) => _db.Checks.Add(check);

    public async Task<List<Check>> GetRecentAsync(Guid monitorId, int limit, CancellationToken ct = default)
        => await _db.Checks.Where(c => c.MonitorId == monitorId)
            .OrderByDescending(c => c.CheckedAt).Take(limit).ToListAsync(ct);

    public async Task<Dictionary<Guid, List<Check>>> GetRecentByMonitorsAsync(
        IEnumerable<Guid> monitorIds,
        int limitPerMonitor,
        CancellationToken ct = default)
    {
        var ids = monitorIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        var checks = await _db.Checks
            .AsNoTracking()
            .Where(c => ids.Contains(c.MonitorId))
            .OrderByDescending(c => c.CheckedAt)
            .ToListAsync(ct);

        return ids.ToDictionary(
            id => id,
            id => checks.Where(c => c.MonitorId == id).Take(limitPerMonitor).ToList());
    }

    public async Task<List<Check>> GetSinceAsync(Guid monitorId, DateTime sinceUtc, CancellationToken ct = default)
        => await _db.Checks
            .AsNoTracking()
            .Where(c => c.MonitorId == monitorId && c.CheckedAt >= sinceUtc)
            .OrderBy(c => c.CheckedAt)
            .ToListAsync(ct);

    public async Task<List<Check>> GetRecentGlobalAsync(int limit, CancellationToken ct = default)
        => await _db.Checks
            .AsNoTracking()
            .Include(c => c.Monitor)
            .Where(c => c.IsUp || c.Attempt == CheckProcessor.MaxAttempts)
            .OrderByDescending(c => c.CheckedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<MonitorPeriodStats> GetPeriodStatsAsync(
        Guid monitorId,
        TimeSpan period,
        CancellationToken ct = default)
    {
        var since = DateTime.UtcNow - period;
        var checks = await _db.Checks
            .AsNoTracking()
            .Where(c => c.MonitorId == monitorId && c.CheckedAt >= since)
            .Where(c => c.IsUp || c.Attempt == CheckProcessor.MaxAttempts)
            .Select(c => new { c.IsUp, c.ResponseMs, c.CheckedAt })
            .ToListAsync(ct);

        if (checks.Count == 0)
            return new MonitorPeriodStats(0, 0, null, null, null, null);

        var up = checks.Count(c => c.IsUp);
        var uptime = up * 100.0 / checks.Count;

        var responseTimes = checks
            .Where(c => c.IsUp && c.ResponseMs is > 0)
            .Select(c => c.ResponseMs!.Value)
            .ToList();

        double? avg = responseTimes.Count > 0 ? responseTimes.Average() : null;

        return new MonitorPeriodStats(
            checks.Count,
            up,
            uptime,
            avg,
            checks.Min(c => c.CheckedAt),
            checks.Max(c => c.CheckedAt));
    }

    public async Task<int> DeleteOlderThanAsync(DateTime utcCutoff, CancellationToken ct = default)
        => await _db.Checks
            .Where(c => c.CheckedAt < utcCutoff)
            .ExecuteDeleteAsync(ct);
}
