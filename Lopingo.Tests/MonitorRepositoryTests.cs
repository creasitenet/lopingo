using Microsoft.EntityFrameworkCore;
using Lopingo.Repositories;

namespace Lopingo.Tests;

public sealed class MonitorRepositoryTests : IDisposable
{
    private readonly TestDbFixture _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Create_persists_monitor()
    {
        await _db.SeedOwnerAsync();
        await using var db = _db.CreateContext();
        var repo = new MonitorRepository(db);

        var monitor = await repo.CreateAsync("https://a.example", 60, []);

        Assert.NotEqual(Guid.Empty, monitor.Id);
        Assert.Equal("https://a.example", monitor.Url);
        Assert.Equal(1, await repo.CountAsync());
    }

    [Fact]
    public async Task List_returns_all_monitors()
    {
        await _db.SeedOwnerAsync();
        await using var db = _db.CreateContext();
        var repo = new MonitorRepository(db);
        await repo.CreateAsync("https://one.example", 60, []);
        await repo.CreateAsync("https://two.example", 60, []);

        var list = await repo.ListAsync();

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task Update_changes_url_and_freq()
    {
        await _db.SeedOwnerAsync();
        await using var db = _db.CreateContext();
        var repo = new MonitorRepository(db);
        var monitor = await repo.CreateAsync("https://old.example", 60, []);

        var ok = await repo.UpdateAsync(monitor.Id, "https://new.example", 300, []);
        var updated = await repo.GetAsync(monitor.Id);

        Assert.True(ok);
        Assert.NotNull(updated);
        Assert.Equal("https://new.example", updated!.Url);
        Assert.Equal(300, updated.FreqSec);
    }

    [Fact]
    public async Task Delete_removes_monitor()
    {
        await _db.SeedOwnerAsync();
        await using var db = _db.CreateContext();
        var repo = new MonitorRepository(db);
        var monitor = await repo.CreateAsync("https://gone.example", 60, []);

        var ok = await repo.DeleteAsync(monitor.Id);

        Assert.True(ok);
        Assert.Equal(0, await repo.CountAsync());
    }

    [Fact]
    public async Task ListAsync_sees_status_updated_by_another_context()
    {
        await _db.SeedOwnerAsync();
        await using var uiDb = _db.CreateContext();
        var uiRepo = new MonitorRepository(uiDb);
        var monitor = await uiRepo.CreateAsync("https://live.example", 60, []);

        Assert.Equal("unknown", (await uiRepo.ListAsync()).Single().Status);

        await using (var workerDb = _db.CreateContext())
        {
            var row = await workerDb.Monitors.FirstAsync(m => m.Id == monitor.Id);
            row.Status = "up";
            row.LastCheckedAt = DateTime.UtcNow;
            await workerDb.SaveChangesAsync();
        }

        Assert.Equal("up", (await uiRepo.ListAsync()).Single().Status);
    }
}
