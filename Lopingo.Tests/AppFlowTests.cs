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

public sealed class AppFlowTests
{
    [Fact]
    public async Task Healthz_returns_ok()
    {
        using var factory = new LopingoWebAppFactory();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Signup_then_second_signup_is_rejected()
    {
        using var factory = new LopingoWebAppFactory();
        using var scope = factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<OwnerAuthService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var owner = await auth.SignupAsync("tester", "secret12");
        Assert.Equal(OwnerAuthService.SingletonOwnerId, owner.Id);
        Assert.Equal(1, await db.Owners.CountAsync());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => auth.SignupAsync("other", "secret12"));
        Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await db.Owners.CountAsync());
    }

    [Fact]
    public async Task Signup_allows_creating_a_monitor()
    {
        using var factory = new LopingoWebAppFactory();
        using var scope = factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<OwnerAuthService>();
        await auth.SignupAsync("tester", "secret12");

        var repo = scope.ServiceProvider.GetRequiredService<MonitorRepository>();
        var monitor = await repo.CreateAsync("https://example.com", 60, []);
        Assert.NotEqual(Guid.Empty, monitor.Id);
    }
}
