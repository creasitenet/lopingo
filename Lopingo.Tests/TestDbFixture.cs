using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Lopingo.Data;
using Lopingo.Data.Entities;
using Monitor = Lopingo.Data.Entities.Monitor;

namespace Lopingo.Tests;

public sealed class TestDbFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDbFixture()
    {
        _connection = new SqliteConnection("Data Source=:memory:;Cache=Shared;Foreign Keys=True");
        _connection.Open();

        using var db = CreateContext();
        db.Database.EnsureCreated();
        if (!db.Owners.Any())
        {
            db.Owners.Add(new Owner
            {
                Id = 1,
                Username = "owner",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            db.SaveChanges();
        }
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AppDbContext(options);
    }

    public IServiceProvider BuildServices(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_connection);
        services.AddDbContext<AppDbContext>((_, o) =>
            o.UseSqlite(_connection).UseSnakeCaseNamingConvention());
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    public async Task<Owner> EnsureOwnerAsync(int id = 1, string username = "owner")
    {
        await using var db = CreateContext();
        var existing = await db.Owners.FindAsync(id);
        if (existing is not null)
            return existing;

        var owner = new Owner
        {
            Id = id,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Owners.Add(owner);
        await db.SaveChangesAsync();
        return owner;
    }

    public async Task<Owner> SeedOwnerAsync(string username = "owner") =>
        await EnsureOwnerAsync(1, username);

    public async Task<Monitor> SeedMonitorAsync(Action<Monitor>? configure = null)
    {
        await EnsureOwnerAsync();
        await using var db = CreateContext();
        var monitor = new Monitor
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com",
            Status = "unknown",
            FreqSec = 60,
            NextRunAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        configure?.Invoke(monitor);
        db.Monitors.Add(monitor);
        await db.SaveChangesAsync();
        return monitor;
    }

    public void Dispose() => _connection.Dispose();
}
