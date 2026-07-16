using System.ComponentModel.DataAnnotations;

namespace Lopingo.Data.Entities;

public class Check
{
    public long Id { get; set; }
    public Guid MonitorId { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public bool IsUp { get; set; }
    public int? ResponseMs { get; set; }
    public int? StatusCode { get; set; }
    public int Attempt { get; set; } = 1;

    [MaxLength(1024)]
    public string? Error { get; set; }

    public Monitor? Monitor { get; set; }
}
