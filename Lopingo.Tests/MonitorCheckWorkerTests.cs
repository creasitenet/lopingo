using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Lopingo.Core.Buses;
using Lopingo.Core.Engine;
using Lopingo.Core.Workers;
using Lopingo.Repositories;
using Monitor = Lopingo.Data.Entities.Monitor;

namespace Lopingo.Tests;

public sealed class MonitorCheckWorkerTests : IDisposable
{
    private readonly TestDbFixture _db = new();

    public void Dispose() => _db.Dispose();

    private MonitorCheckWorker CreateWorker(HttpMessageHandler handler)
    {
        var services = _db.BuildServices(s =>
        {
            s.AddScoped<CheckRepository>();
            s.AddScoped<IncidentRepository>();
            s.AddSingleton<MonitorEventsBus>();
            s.AddSingleton(new MonitorCheckWorkerOptions
            {
                MaxParallelism = 1,
                MaxMonitorsPerTick = 100,
                TickInterval = TimeSpan.FromHours(1),
            });
            s.AddSingleton(_ => new CheckProcessor(
                new HttpClient(handler),
                NullLogger<CheckProcessor>.Instance,
                TimeSpan.Zero));
        });

        return new MonitorCheckWorker(
            services.GetRequiredService<IServiceScopeFactory>(),
            services.GetRequiredService<MonitorEventsBus>(),
            NullLogger<MonitorCheckWorker>.Instance,
            services.GetRequiredService<MonitorCheckWorkerOptions>());
    }

    [Fact]
    public async Task Tick_does_nothing_when_no_monitors_are_due()
    {
        await _db.EnsureOwnerAsync();
        await using var db = _db.CreateContext();
        db.Monitors.Add(new Monitor
        {
            Id = Guid.NewGuid(),
            Url = "https://later.example",
            FreqSec = 60,
            Status = "unknown",
            NextRunAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var worker = CreateWorker(StubHttpMessageHandler.Always(HttpStatusCode.OK));
        await worker.RunTickForTestsAsync(CancellationToken.None);

        Assert.Equal(0, await db.Checks.CountAsync());
    }

    [Fact]
    public async Task Tick_processes_due_monitors()
    {
        await _db.EnsureOwnerAsync();
        await using var db = _db.CreateContext();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        db.Monitors.AddRange(
            new Monitor
            {
                Id = id1,
                Url = "https://one.example",
                FreqSec = 60,
                Status = "unknown",
                NextRunAt = DateTime.UtcNow.AddSeconds(-5),
                CreatedAt = DateTime.UtcNow,
            },
            new Monitor
            {
                Id = id2,
                Url = "https://two.example",
                FreqSec = 60,
                Status = "unknown",
                NextRunAt = DateTime.UtcNow.AddSeconds(-1),
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync();

        var worker = CreateWorker(StubHttpMessageHandler.Always(HttpStatusCode.OK));
        await worker.RunTickForTestsAsync(CancellationToken.None);

        await using var verify = _db.CreateContext();
        Assert.Equal(2, await verify.Checks.CountAsync());
        var m1 = await verify.Monitors.FindAsync(id1);
        var m2 = await verify.Monitors.FindAsync(id2);
        Assert.Equal("up", m1!.Status);
        Assert.Equal("up", m2!.Status);
        Assert.True(m1.NextRunAt > DateTime.UtcNow);
    }
}
