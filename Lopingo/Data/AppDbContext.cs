using Microsoft.EntityFrameworkCore;
using Check = Lopingo.Data.Entities.Check;
using Incident = Lopingo.Data.Entities.Incident;
using Monitor = Lopingo.Data.Entities.Monitor;
using Owner = Lopingo.Data.Entities.Owner;
using Telegram = Lopingo.Data.Entities.Telegram;

namespace Lopingo.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Telegram> Telegrams => Set<Telegram>();
    public DbSet<Monitor> Monitors => Set<Monitor>();
    public DbSet<Check> Checks => Set<Check>();
    public DbSet<Incident> Incidents => Set<Incident>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Single-owner instance: Id is always 1 (not database-generated).
        b.Entity<Owner>()
            .Property(x => x.Id)
            .ValueGeneratedNever();
        b.Entity<Owner>()
            .HasIndex(x => x.Username)
            .IsUnique();

        b.Entity<Monitor>()
            .HasIndex(x => x.NextRunAt);

        b.Entity<Check>()
            .HasIndex(x => new { x.MonitorId, x.CheckedAt });
        b.Entity<Check>()
            .HasOne(x => x.Monitor)
            .WithMany()
            .HasForeignKey(x => x.MonitorId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Incident>()
            .HasIndex(x => x.MonitorId);
        b.Entity<Incident>()
            .HasIndex(x => new { x.MonitorId, x.EndedAt });
        b.Entity<Incident>()
            .HasOne(x => x.Monitor)
            .WithMany()
            .HasForeignKey(x => x.MonitorId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Telegram>()
            .HasIndex(x => x.Name);

        b.Entity<Monitor>()
            .HasMany(m => m.Telegrams)
            .WithMany(t => t.Monitors);
    }
}
