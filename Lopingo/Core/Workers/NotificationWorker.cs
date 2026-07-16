using Microsoft.EntityFrameworkCore;
using Lopingo.Core.Buses;
using Lopingo.Data;
using Lopingo.Repositories;
using Lopingo.Services.Notifications;
using Incident = Lopingo.Data.Entities.Incident;
using Monitor = Lopingo.Data.Entities.Monitor;

namespace Lopingo.Core.Workers;

public sealed class NotificationWorker : BackgroundService
{
    private readonly MonitorEventsBus _bus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationWorker> _log;

    public NotificationWorker(
        MonitorEventsBus bus,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationWorker> log)
    {
        _bus = bus;
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("NotificationWorker started");

        await foreach (var evt in _bus.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await HandleAsync(evt, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Notification dispatch failed for monitor {MonitorId}", evt.Monitor.Id);
            }
        }

        _log.LogInformation("NotificationWorker stopped");
    }

    private async Task HandleAsync(MonitorUpdated evt, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var telegrams = scope.ServiceProvider.GetRequiredService<TelegramRepository>();
        var notifier = scope.ServiceProvider.GetRequiredService<ITelegramNotifier>();

        var enabled = await telegrams.ListEnabledForMonitorAsync(evt.Monitor.Id, ct);
        if (enabled.Count == 0) return;

        var monitor = await db.Monitors.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == evt.Monitor.Id, ct);
        if (monitor is null) return;

        string message;
        if (evt.TransitionedToDown)
            message = FormatDown(monitor, evt.Incident);
        else if (evt.Incident?.EndedAt is not null)
            message = FormatUp(monitor, evt.Incident);
        else
            return;

        foreach (var telegram in enabled)
        {
            try
            {
                await notifier.SendAsync(telegram.BotToken, telegram.ChatId, message, ct);
                _log.LogInformation("Telegram alert sent via {Name} for {Url}", telegram.Name, monitor.Url);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Telegram {Name} ({Id}) failed for {Url}", telegram.Name, telegram.Id, monitor.Url);
            }
        }
    }

    private static string FormatDown(Monitor monitor, Incident? incident)
    {
        var status = monitor.LastStatusCode?.ToString() ?? "n/a";
        var error = incident?.FirstError ?? monitor.LastError ?? "unknown";
        return $"🔴 [Lopingo] Monitor {monitor.Url} is DOWN (HTTP {status}, {error})";
    }

    private static string FormatUp(Monitor monitor, Incident incident)
    {
        var duration = FormatDuration(incident.DurationSec);
        return $"🟢 [Lopingo] Monitor {monitor.Url} is back UP. Downtime: {duration}.";
    }

    private static string FormatDuration(long? seconds)
    {
        if (seconds is null or <= 0) return "0s";
        var ts = TimeSpan.FromSeconds(seconds.Value);
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }
}
