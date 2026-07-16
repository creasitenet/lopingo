using System.Diagnostics;
using System.Net;
using Lopingo.Data;
using Lopingo.Repositories;
using Check = Lopingo.Data.Entities.Check;
using Incident = Lopingo.Data.Entities.Incident;
using Monitor = Lopingo.Data.Entities.Monitor;

namespace Lopingo.Core.Engine;

public sealed class CheckProcessor
{
    public const int MaxAttempts = 4;
    public static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(10);

    private static readonly HttpStatusCode[] GetFallbackCodes =
        { HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotImplemented };

    private readonly HttpClient _http;
    private readonly ILogger<CheckProcessor> _log;
    private readonly TimeSpan _retryDelay;
    private readonly TimeSpan _probeTimeout;

    public CheckProcessor(
        HttpClient http,
        ILogger<CheckProcessor> log,
        TimeSpan? retryDelay = null,
        TimeSpan? probeTimeout = null)
    {
        _http = http;
        _log = log;
        _retryDelay = retryDelay ?? DefaultRetryDelay;
        _probeTimeout = probeTimeout ?? ProbeTimeout;
    }

    public async Task<(Incident? Incident, bool TransitionedToDown)> ProcessAsync(
        Monitor monitor,
        string previousStatus,
        CheckRepository checks,
        IncidentRepository incidents,
        AppDbContext db,
        CancellationToken ct)
    {
        var (status, statusCode, responseMs, error) =
            await CheckWithRetryAsync(checks, monitor.Id, monitor.Url, ct);

        var now = DateTime.UtcNow;
        var isUp = status == "up";

        monitor.LastStatusCode = statusCode;
        monitor.LastResponseMs = responseMs;
        monitor.LastError = error;
        monitor.Status = status;
        monitor.LastCheckedAt = now;

        if (previousStatus != "up" && isUp)
        {
            var open = await incidents.GetOpenByMonitorAsync(monitor.Id, ct);
            if (open is not null)
            {
                incidents.Close(open, now, error);
                return (open, false);
            }
        }
        else if (previousStatus != "down" && !isUp)
        {
            var existing = await incidents.GetOpenByMonitorAsync(monitor.Id, ct);
            if (existing is null)
            {
                var opened = incidents.Open(monitor.Id, error, now);
                await db.SaveChangesAsync(ct);
                return (opened, true);
            }
        }

        return (null, false);
    }

    private async Task<(string status, int? code, int? ms, string? error)>
        CheckWithRetryAsync(CheckRepository checks, Guid monitorId, string url, CancellationToken ct)
    {
        ProbeResult? last = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var r = await ProbeOnceAsync(url, ct);
            last = r;

            checks.Add(new Check
            {
                MonitorId = monitorId,
                IsUp = r.IsUp,
                ResponseMs = r.ResponseMs,
                StatusCode = r.StatusCode,
                Attempt = attempt,
                Error = r.Error,
                CheckedAt = DateTime.UtcNow,
            });

            if (r.IsUp) return ("up", r.StatusCode, r.ResponseMs, null);

            if (attempt < MaxAttempts)
            {
                try { await Task.Delay(_retryDelay, ct); }
                catch (OperationCanceledException) { return ("down", r.StatusCode, r.ResponseMs, r.Error); }
            }
        }

        return ("down", last?.StatusCode, last?.ResponseMs, last?.Error);
    }

    private async Task<ProbeResult> ProbeOnceAsync(string url, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_probeTimeout);
        try
        {
            using var head = new HttpRequestMessage(HttpMethod.Head, url);
            using var resp = await _http.SendAsync(head, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            if (Array.IndexOf(GetFallbackCodes, resp.StatusCode) >= 0)
            {
                using var get = new HttpRequestMessage(HttpMethod.Get, url);
                using var resp2 = await _http.SendAsync(get, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                sw.Stop();
                var status2 = (int)resp2.StatusCode;
                return new ProbeResult(status2 is >= 200 and < 400, status2, (int)sw.ElapsedMilliseconds, null);
            }

            sw.Stop();
            var status = (int)resp.StatusCode;
            return new ProbeResult(status is >= 200 and < 400, status, (int)sw.ElapsedMilliseconds, null);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            sw.Stop();
            return new ProbeResult(false, null, (int)sw.ElapsedMilliseconds, "timeout");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            return new ProbeResult(false, null, (int)sw.ElapsedMilliseconds, Classify(ex));
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ProbeResult(false, null, (int)sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static string Classify(HttpRequestException ex) => ex.HttpRequestError switch
    {
        HttpRequestError.NameResolutionError => "dns",
        HttpRequestError.ConnectionError => "connection refused",
        HttpRequestError.SecureConnectionError => "ssl/tls",
        HttpRequestError.UserAuthenticationError => "auth",
        HttpRequestError.HttpProtocolError => $"http {ex.StatusCode.GetValueOrDefault():D}",
        _ => ex.Message,
    };

    internal sealed record ProbeResult(bool IsUp, int? StatusCode, int? ResponseMs, string? Error);
}
