using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Lopingo.Core.Buses;
using Lopingo.Core.Engine;
using Lopingo.Core.Workers;
using Lopingo.Data;
using Lopingo.Repositories;
using Lopingo.Services.Auth;
using Lopingo.Services.Notifications;

var builder = WebApplication.CreateBuilder(args);

var dbPath = builder.Configuration["LOPINGO_DB_PATH"]
    ?? Environment.GetEnvironmentVariable("LOPINGO_DB_PATH")
    ?? (builder.Environment.IsDevelopment()
        ? Path.Combine(builder.Environment.ContentRootPath, "lopingo.db")
        : "/data/lopingo.db");

var dbDir = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDir)) Directory.CreateDirectory(dbDir);

var connStr = $"Data Source={dbPath};Cache=Shared;Foreign Keys=True";
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(connStr)
     .UseSnakeCaseNamingConvention());

builder.Services
    .AddAuthentication(OwnerCookieService.AuthenticationScheme)
    .AddCookie(OwnerCookieService.AuthenticationScheme, opts =>
    {
        opts.Cookie.Name = "lopingo.auth";
        opts.Cookie.HttpOnly = true;
        opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        opts.Cookie.SameSite = SameSiteMode.Lax;
        opts.ExpireTimeSpan = TimeSpan.FromDays(7);
        opts.SlidingExpiration = true;
        opts.LoginPath = "/login";
        opts.LogoutPath = "/logout";
        opts.AccessDeniedPath = "/login";
        opts.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/healthz"))
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            else
                ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<OwnerAuthService>();
builder.Services.AddScoped<OwnerCookieService>();
builder.Services.AddSingleton<LoginThrottle>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<MonitorRepository>();
builder.Services.AddScoped<CheckRepository>();
builder.Services.AddScoped<IncidentRepository>();
builder.Services.AddScoped<TelegramRepository>();

builder.Services.AddHttpClient<CheckProcessor>(c => c.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddHttpClient<ITelegramNotifier, TelegramNotifier>(c => c.Timeout = TimeSpan.FromSeconds(15));

builder.Services.AddSingleton<MonitorEventsBus>();
builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var tick = TimeSpan.FromSeconds(Math.Clamp(cfg.GetValue("LOPINGO_TICK_SECONDS", 5), 1, 600));
    var maxPar = Math.Clamp(cfg.GetValue("LOPINGO_MAX_PARALLEL", 10), 1, 256);
    var maxBatch = Math.Clamp(cfg.GetValue("LOPINGO_BATCH", 100), 1, 1000);
    return new MonitorCheckWorkerOptions
    {
        TickInterval = tick,
        MaxMonitorsPerTick = maxBatch,
        MaxParallelism = maxPar,
    };
});
builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var days = Math.Clamp(cfg.GetValue("LOPINGO_CHECK_RETENTION_DAYS", 30), 1, 3650);
    return new CheckPruneWorkerOptions { RetentionDays = days };
});
builder.Services.AddHostedService<MonitorCheckWorker>();
builder.Services.AddHostedService<NotificationWorker>();
builder.Services.AddHostedService<CheckPruneWorker>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddMudServices();

builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opts.KnownIPNetworks.Clear();
    opts.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.MapStaticAssets();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapGet("/monitors", () => Results.Redirect("/"));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapRazorComponents<Lopingo.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program { }
