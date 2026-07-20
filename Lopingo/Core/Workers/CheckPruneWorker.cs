using Lopingo.Repositories;

namespace Lopingo.Core.Workers;

public sealed class CheckPruneWorkerOptions
{
    public int RetentionDays { get; set; } = 30;
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(24);
}

public sealed class CheckPruneWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CheckPruneWorkerOptions _opts;
    private readonly ILogger<CheckPruneWorker> _log;

    public CheckPruneWorker(
        IServiceScopeFactory scopeFactory,
        CheckPruneWorkerOptions opts,
        ILogger<CheckPruneWorker> log)
    {
        _scopeFactory = scopeFactory;
        _opts = opts;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation(
            "CheckPruneWorker started (retention={Days}d, interval={Hours}h)",
            _opts.RetentionDays, _opts.Interval.TotalHours);

        // Run once at startup so a restart after months away still cleans up.
        await PruneAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_opts.Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await PruneAsync(stoppingToken);
        }

        _log.LogInformation("CheckPruneWorker stopped");
    }

    private async Task PruneAsync(CancellationToken ct)
    {
        if (_opts.RetentionDays <= 0) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var checks = scope.ServiceProvider.GetRequiredService<CheckRepository>();
            var cutoff = DateTime.UtcNow.AddDays(-_opts.RetentionDays);
            var deleted = await checks.DeleteOlderThanAsync(cutoff, ct);

            if (deleted > 0)
                _log.LogInformation("Pruned {Count} checks older than {Days} days", deleted, _opts.RetentionDays);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Check prune failed");
        }
    }
}
