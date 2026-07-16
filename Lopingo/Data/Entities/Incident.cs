using System.ComponentModel.DataAnnotations;

namespace Lopingo.Data.Entities;

public class Incident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MonitorId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public long? DurationSec { get; set; }
    [MaxLength(1024)]
    public string? FirstError { get; set; }
    [MaxLength(1024)]
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Monitor? Monitor { get; set; }
}