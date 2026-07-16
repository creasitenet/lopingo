using Lopingo.Repositories;

namespace Lopingo.Tests;

public sealed class IncidentRepositoryTests : IDisposable
{
    private readonly TestDbFixture _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Open_creates_incident_without_end()
    {
        var monitor = await _db.SeedMonitorAsync();
        await using var db = _db.CreateContext();
        var repo = new IncidentRepository(db);
        var now = DateTime.UtcNow;

        var incident = repo.Open(monitor.Id, "connection refused", now);
        await db.SaveChangesAsync();

        var open = await repo.GetOpenByMonitorAsync(monitor.Id);
        Assert.NotNull(open);
        Assert.Equal(incident.Id, open!.Id);
        Assert.Null(open.EndedAt);
        Assert.Equal("connection refused", open.FirstError);
    }

    [Fact]
    public async Task Close_sets_duration_and_end()
    {
        var monitor = await _db.SeedMonitorAsync();
        await using var db = _db.CreateContext();
        var repo = new IncidentRepository(db);
        var started = DateTime.UtcNow.AddMinutes(-2);
        var ended = DateTime.UtcNow;

        var incident = repo.Open(monitor.Id, "timeout", started);
        await db.SaveChangesAsync();

        repo.Close(incident, ended, "still down");
        await db.SaveChangesAsync();

        Assert.NotNull(incident.EndedAt);
        Assert.Equal("still down", incident.LastError);
        Assert.True(incident.DurationSec >= 119);
        Assert.Null(await repo.GetOpenByMonitorAsync(monitor.Id));
    }

    [Fact]
    public async Task ListByMonitor_returns_most_recent_first()
    {
        var monitor = await _db.SeedMonitorAsync();
        await using var db = _db.CreateContext();
        var repo = new IncidentRepository(db);
        var older = DateTime.UtcNow.AddHours(-2);
        var newer = DateTime.UtcNow.AddHours(-1);

        var first = repo.Open(monitor.Id, "a", older);
        await db.SaveChangesAsync();
        repo.Close(first, newer, "a");
        repo.Open(monitor.Id, "b", newer);
        await db.SaveChangesAsync();

        var list = await repo.ListByMonitorAsync(monitor.Id, 10);
        Assert.Equal(2, list.Count);
        Assert.Equal("b", list[0].FirstError);
    }
}
