using Microsoft.EntityFrameworkCore;
using Lopingo.Data;
using Monitor = Lopingo.Data.Entities.Monitor;

namespace Lopingo.Repositories;

public sealed class MonitorRepository
{
    private readonly AppDbContext _db;

    public MonitorRepository(AppDbContext db) => _db = db;

    public async Task<List<Monitor>> ListAsync(CancellationToken ct = default)
        => await _db.Monitors.OrderBy(m => m.CreatedAt).ToListAsync(ct);

    public async Task<Monitor?> GetAsync(Guid id, CancellationToken ct = default)
        => await _db.Monitors.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<Monitor?> GetWithTelegramsAsync(Guid id, CancellationToken ct = default)
        => await _db.Monitors
            .Include(m => m.Telegrams)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<Monitor> CreateAsync(
        string url, int freqSec, IReadOnlyList<Guid> telegramIds, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            Url = url.Trim(),
            FreqSec = freqSec,
            Status = "unknown",
            NextRunAt = now,
            CreatedAt = now,
        };
        _db.Monitors.Add(monitor);
        await AttachTelegramsAsync(monitor, telegramIds, ct);
        await _db.SaveChangesAsync(ct);
        return monitor;
    }

    public async Task<bool> UpdateAsync(
        Guid id, string url, int freqSec, IReadOnlyList<Guid> telegramIds, CancellationToken ct = default)
    {
        var monitor = await _db.Monitors
            .Include(x => x.Telegrams)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (monitor is null) return false;
        monitor.Url = url.Trim();
        monitor.FreqSec = freqSec;
        await AttachTelegramsAsync(monitor, telegramIds, ct);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var monitor = await GetAsync(id, ct);
        if (monitor is null) return false;
        _db.Monitors.Remove(monitor);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<int> CountAsync(CancellationToken ct = default)
        => _db.Monitors.CountAsync(ct);

    private async Task AttachTelegramsAsync(Monitor monitor, IReadOnlyList<Guid> telegramIds, CancellationToken ct)
    {
        monitor.Telegrams.Clear();
        if (telegramIds.Count == 0) return;

        var telegrams = await _db.Telegrams
            .Where(t => telegramIds.Contains(t.Id))
            .ToListAsync(ct);
        foreach (var telegram in telegrams)
            monitor.Telegrams.Add(telegram);
    }
}
