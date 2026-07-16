using Microsoft.EntityFrameworkCore;
using Lopingo.Core.Buses;
using Lopingo.Core.Engine;
using Lopingo.Data;
using Lopingo.Repositories;
using Incident = Lopingo.Data.Entities.Incident;
using Monitor = Lopingo.Data.Entities.Monitor;

namespace Lopingo.Core.Workers;

public sealed class MonitorCheckWorkerOptions
{
    public TimeSpan TickInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int MaxMonitorsPerTick { get; set; } = 100;
    public int MaxParallelism { get; set; } = 10;
}

public sealed class MonitorCheckWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MonitorEventsBus _bus;
    private readonly ILogger<MonitorCheckWorker> _log;
    private readonly MonitorCheckWorkerOptions _opts;

    public MonitorCheckWorker(
        IServiceScopeFactory scopeFactory,
        MonitorEventsBus bus,
        ILogger<MonitorCheckWorker> log,
        MonitorCheckWorkerOptions opts)
    {
        _scopeFactory = scopeFactory;
        _bus = bus;
        _log = log;
        _opts = opts;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation(
            "MonitorCheckWorker started (tick={Tick}s, parallelism={Par}, batch={Batch})",
            _opts.TickInterval.TotalSeconds, _opts.MaxParallelism, _opts.MaxMonitorsPerTick);

        try { await TickAsync(stoppingToken); }
        catch (OperationCanceledException) { return; }
        catch (Exception ex) { _log.LogError(ex, "Warmup tick failed"); }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await Task.Delay(_opts.TickInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }

            try { await TickAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _log.LogError(ex, "Tick failed"); }
        }

        _log.LogInformation("MonitorCheckWorker stopped");
    }

    internal Task RunTickForTestsAsync(CancellationToken ct) => TickAsync(ct);

    private async Task TickAsync(CancellationToken ct)
    {
        List<Guid> dueIds;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;
            dueIds = await db.Monitors
                .Where(m => m.NextRunAt <= now)
                .OrderBy(m => m.NextRunAt)
                .Take(_opts.MaxMonitorsPerTick)
                .Select(m => m.Id)
                .ToListAsync(ct);
        }

        if (dueIds.Count == 0) return;

        await Parallel.ForEachAsync(
            dueIds,
            new ParallelOptions { MaxDegreeOfParallelism = _opts.MaxParallelism, CancellationToken = ct },
            (id, token) => ProcessOneAsync(id, token));
    }

    private async ValueTask ProcessOneAsync(Guid monitorId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var checks = scope.ServiceProvider.GetRequiredService<CheckRepository>();
        var incidents = scope.ServiceProvider.GetRequiredService<IncidentRepository>();
        var checkProcessor = scope.ServiceProvider.GetRequiredService<CheckProcessor>();

        Incident? persistedIncident = null;
        bool transitionedToDown = false;

        try
        {
            var monitor = await db.Monitors.FirstOrDefaultAsync(m => m.Id == monitorId, ct);
            if (monitor is null) return;

            var previousStatus = monitor.Status;
            (persistedIncident, transitionedToDown) = await checkProcessor.ProcessAsync(
                monitor, previousStatus, checks, incidents, db, ct);

            monitor.NextRunAt = DateTime.UtcNow.AddSeconds(monitor.FreqSec);
            await db.SaveChangesAsync(ct);
            await _bus.CheckedWriter.WriteAsync(monitorId, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(ex, "Check failed for monitor {Id}", monitorId);
            try
            {
                var monitor = await db.Monitors.FirstOrDefaultAsync(m => m.Id == monitorId, ct);
                if (monitor is not null)
                {
                    monitor.Status = "down";
                    monitor.LastError = ex.Message;
                    monitor.LastCheckedAt = DateTime.UtcNow;
                    monitor.NextRunAt = DateTime.UtcNow.AddSeconds(Math.Max(monitor.FreqSec, 30));
                    await db.SaveChangesAsync(ct);
                    await _bus.CheckedWriter.WriteAsync(monitorId, ct);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { /* next tick will retry */ }
            return;
        }

        if (transitionedToDown || persistedIncident?.EndedAt is not null)
        {
            await _bus.Writer.WriteAsync(new MonitorUpdated(
                Monitor: new Monitor { Id = monitorId, Url = string.Empty },
                Incident: persistedIncident,
                TransitionedToDown: transitionedToDown), ct);
        }
    }
}
