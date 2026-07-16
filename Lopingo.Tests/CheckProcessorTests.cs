using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Lopingo.Core.Engine;
using Lopingo.Data.Entities;
using Lopingo.Repositories;

namespace Lopingo.Tests;

public sealed class CheckProcessorTests : IDisposable
{
    private readonly TestDbFixture _db = new();

    public void Dispose() => _db.Dispose();

    private CheckProcessor CreateProcessor(
        HttpMessageHandler handler,
        TimeSpan? probeTimeout = null)
    {
        var http = new HttpClient(handler);
        return new CheckProcessor(http, NullLogger<CheckProcessor>.Instance, TimeSpan.Zero, probeTimeout);
    }

    [Fact]
    public async Task Head_2xx_marks_monitor_up()
    {
        var processor = CreateProcessor(StubHttpMessageHandler.Always(HttpStatusCode.OK));
        var monitor = await _db.SeedMonitorAsync();
        await using var db = _db.CreateContext();
        monitor = (await db.Monitors.FindAsync(monitor.Id))!;

        var checks = new CheckRepository(db);
        var incidents = new IncidentRepository(db);
        var (_, transitioned) = await processor.ProcessAsync(monitor, "unknown", checks, incidents, db, default);
        await db.SaveChangesAsync();

        Assert.False(transitioned);
        Assert.Equal("up", monitor.Status);
        Assert.Equal(200, monitor.LastStatusCode);
        Assert.Equal(1, await db.Checks.CountAsync(c => c.MonitorId == monitor.Id));
    }

    [Fact]
    public async Task Head_4xx_marks_monitor_down_after_retries()
    {
        var processor = CreateProcessor(StubHttpMessageHandler.Always(HttpStatusCode.NotFound));
        var monitor = await _db.SeedMonitorAsync();
        await using var db = _db.CreateContext();
        monitor = (await db.Monitors.FindAsync(monitor.Id))!;

        var checks = new CheckRepository(db);
        var incidents = new IncidentRepository(db);
        var (_, transitioned) = await processor.ProcessAsync(monitor, "unknown", checks, incidents, db, default);
        await db.SaveChangesAsync();

        Assert.True(transitioned);
        Assert.Equal("down", monitor.Status);
        Assert.Equal(404, monitor.LastStatusCode);
        Assert.Equal(CheckProcessor.MaxAttempts, await db.Checks.CountAsync(c => c.MonitorId == monitor.Id));
    }

    [Fact]
    public async Task Head_5xx_marks_monitor_down()
    {
        var processor = CreateProcessor(StubHttpMessageHandler.Always(HttpStatusCode.InternalServerError));
        var monitor = await _db.SeedMonitorAsync();
        await using var db = _db.CreateContext();
        monitor = (await db.Monitors.FindAsync(monitor.Id))!;

        var checks = new CheckRepository(db);
        var incidents = new IncidentRepository(db);
        await processor.ProcessAsync(monitor, "unknown", checks, incidents, db, default);
        await db.SaveChangesAsync();

        Assert.Equal("down", monitor.Status);
        Assert.Equal(500, monitor.LastStatusCode);
    }

    [Fact]
    public async Task Head_405_falls_back_to_get()
    {
        var handler = new StubHttpMessageHandler((req, _) =>
        {
            if (req.Method == HttpMethod.Head)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.MethodNotAllowed));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        var processor = CreateProcessor(handler);
        var monitor = await _db.SeedMonitorAsync();
        await using var db = _db.CreateContext();
        monitor = (await db.Monitors.FindAsync(monitor.Id))!;

        var checks = new CheckRepository(db);
        var incidents = new IncidentRepository(db);
        await processor.ProcessAsync(monitor, "unknown", checks, incidents, db, default);
        await db.SaveChangesAsync();

        Assert.Equal("up", monitor.Status);
        Assert.Equal(200, monitor.LastStatusCode);
        Assert.Equal(1, await db.Checks.CountAsync(c => c.MonitorId == monitor.Id));
    }

    [Fact]
    public async Task Head_501_falls_back_to_get_and_records_down()
    {
        var handler = new StubHttpMessageHandler((req, _) =>
        {
            if (req.Method == HttpMethod.Head)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway));
        });
        var processor = CreateProcessor(handler);
        var monitor = await _db.SeedMonitorAsync();
        await using var db = _db.CreateContext();
        monitor = (await db.Monitors.FindAsync(monitor.Id))!;

        var checks = new CheckRepository(db);
        var incidents = new IncidentRepository(db);
        await processor.ProcessAsync(monitor, "unknown", checks, incidents, db, default);
        await db.SaveChangesAsync();

        Assert.Equal("down", monitor.Status);
        Assert.Equal(502, monitor.LastStatusCode);
    }

    [Fact]
    public async Task Timeout_records_down_with_timeout_error()
    {
        var handler = new StubHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var processor = CreateProcessor(handler, TimeSpan.FromMilliseconds(50));
        var monitor = await _db.SeedMonitorAsync();
        await using var db = _db.CreateContext();
        monitor = (await db.Monitors.FindAsync(monitor.Id))!;

        var checks = new CheckRepository(db);
        var incidents = new IncidentRepository(db);
        await processor.ProcessAsync(monitor, "unknown", checks, incidents, db, default);
        await db.SaveChangesAsync();

        Assert.Equal("down", monitor.Status);
        Assert.Equal("timeout", monitor.LastError);
    }

    [Fact]
    public async Task Recovery_closes_open_incident()
    {
        var processor = CreateProcessor(StubHttpMessageHandler.Always(HttpStatusCode.OK));
        var started = DateTime.UtcNow.AddMinutes(-5);
        var monitor = await _db.SeedMonitorAsync(m =>
        {
            m.Status = "down";
        });
        await using var db = _db.CreateContext();
        monitor = (await db.Monitors.FindAsync(monitor.Id))!;
        db.Incidents.Add(new Incident
        {
            Id = Guid.NewGuid(),
            MonitorId = monitor.Id,
            StartedAt = started,
            FirstError = "timeout",
            CreatedAt = started,
        });
        await db.SaveChangesAsync();

        var checks = new CheckRepository(db);
        var incidents = new IncidentRepository(db);
        var (closed, transitioned) = await processor.ProcessAsync(monitor, "down", checks, incidents, db, default);
        await db.SaveChangesAsync();

        Assert.False(transitioned);
        Assert.Equal("up", monitor.Status);
        Assert.NotNull(closed);
        Assert.NotNull(closed!.EndedAt);
        Assert.True(closed.DurationSec > 0);
    }
}
