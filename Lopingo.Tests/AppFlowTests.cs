using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Lopingo.Data;
using Lopingo.Repositories;
using Lopingo.Services.Auth;

namespace Lopingo.Tests;

public sealed class LopingoWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"lopingo-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("LOPINGO_DB_PATH", _dbPath);

        builder.ConfigureServices(services =>
        {
            foreach (var d in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                services.Remove(d);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try { File.Delete(_dbPath); } catch { /* best effort */ }
        }
        base.Dispose(disposing);
    }
}

public sealed class AppFlowTests : IClassFixture<LopingoWebAppFactory>
{
    private readonly LopingoWebAppFactory _factory;

    public AppFlowTests(LopingoWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Healthz_returns_ok()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Signup_creates_owner_and_allows_monitor_create()
    {
        using var scope = _factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<OwnerAuthService>();
        await auth.SignupAsync("tester", "secret12");

        var owners = scope.ServiceProvider.GetRequiredService<AppDbContext>().Owners;
        Assert.Equal(1, await owners.CountAsync());

        var repo = scope.ServiceProvider.GetRequiredService<MonitorRepository>();
        var monitor = await repo.CreateAsync("https://example.com", 60, []);
        Assert.NotEqual(Guid.Empty, monitor.Id);
    }
}
