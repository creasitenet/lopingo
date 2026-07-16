using Microsoft.EntityFrameworkCore;
using Lopingo.Core.Engine;
using Lopingo.Data.Entities;
using Lopingo.Repositories;

namespace Lopingo.Tests;

public sealed class CheckRepositoryTests : IDisposable
{
    private readonly TestDbFixture _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetPeriodStats_returns_nulls_when_no_checks()
    {
        var monitor = await _db.SeedMonitorAsync();
        await using var db = _db.CreateContext();
        var repo = new CheckRepository(db);

        var stats = await repo.GetPeriodStatsAsync(monitor.Id, TimeSpan.FromHours(24));

        Assert.Equal(0, stats.ResultChecks);
        Assert.Null(stats.UptimePercent);
        Assert.Null(stats.AvgResponseMs);
    }

    [Fact]
    public async Task GetPeriodStats_ignores_retry_attempts_and_averages_response()
    {
        var monitor = await _db.SeedMonitorAsync();
        var now = DateTime.UtcNow;
        await using var db = _db.CreateContext();

        db.Checks.AddRange(
            new Check
            {
                MonitorId = monitor.Id,
                CheckedAt = now.AddMinutes(-10),
                IsUp = false,
                Attempt = 1,
                ResponseMs = 120,
            },
            new Check
            {
                MonitorId = monitor.Id,
                CheckedAt = now.AddMinutes(-10).AddSeconds(5),
                IsUp = false,
                Attempt = 2,
                ResponseMs = 130,
            },
            new Check
            {
                MonitorId = monitor.Id,
                CheckedAt = now.AddMinutes(-10).AddSeconds(10),
                IsUp = true,
                Attempt = 3,
                ResponseMs = 100,
            },
            new Check
            {
                MonitorId = monitor.Id,
                CheckedAt = now.AddMinutes(-5),
                IsUp = true,
                Attempt = 1,
                ResponseMs = 200,
            });

        await db.SaveChangesAsync();

        var repo = new CheckRepository(db);
        var stats = await repo.GetPeriodStatsAsync(monitor.Id, TimeSpan.FromHours(24));

        Assert.Equal(2, stats.ResultChecks);
        Assert.Equal(2, stats.UpChecks);
        Assert.Equal(100, stats.UptimePercent);
        Assert.Equal(150, stats.AvgResponseMs);
    }

    [Fact]
    public async Task GetPeriodStats_counts_final_failed_attempt_only()
    {
        var monitor = await _db.SeedMonitorAsync();
        var now = DateTime.UtcNow;
        await using var db = _db.CreateContext();

        for (var attempt = 1; attempt <= CheckProcessor.MaxAttempts; attempt++)
        {
            db.Checks.Add(new Check
            {
                MonitorId = monitor.Id,
                CheckedAt = now.AddMinutes(-1).AddSeconds(attempt),
                IsUp = false,
                Attempt = attempt,
                ResponseMs = 50,
                Error = "down",
            });
        }

        await db.SaveChangesAsync();

        var repo = new CheckRepository(db);
        var stats = await repo.GetPeriodStatsAsync(monitor.Id, TimeSpan.FromHours(24));

        Assert.Equal(1, stats.ResultChecks);
        Assert.Equal(0, stats.UpChecks);
        Assert.Equal(0, stats.UptimePercent);
        Assert.Null(stats.AvgResponseMs);
    }

    [Fact]
    public async Task DeleteOlderThan_removes_only_old_checks()
    {
        var monitor = await _db.SeedMonitorAsync();
        var now = DateTime.UtcNow;
        await using var db = _db.CreateContext();

        db.Checks.AddRange(
            new Check
            {
                MonitorId = monitor.Id,
                CheckedAt = now.AddDays(-40),
                IsUp = true,
                Attempt = 1,
                ResponseMs = 100,
            },
            new Check
            {
                MonitorId = monitor.Id,
                CheckedAt = now.AddDays(-5),
                IsUp = true,
                Attempt = 1,
                ResponseMs = 110,
            });
        await db.SaveChangesAsync();

        var repo = new CheckRepository(db);
        var deleted = await repo.DeleteOlderThanAsync(now.AddDays(-30));

        Assert.Equal(1, deleted);
        Assert.Equal(1, await db.Checks.CountAsync());
    }
}
