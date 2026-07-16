using System.ComponentModel.DataAnnotations;

namespace Lopingo.Data.Entities;

public sealed class Monitor
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(2048), Url]
    public string Url { get; set; } = "";

    public int FreqSec { get; set; } = 60;

    [Required, MaxLength(16)]
    public string Status { get; set; } = "unknown";

    public int? LastStatusCode { get; set; }
    public int? LastResponseMs { get; set; }
    [MaxLength(1024)]
    public string? LastError { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public DateTime NextRunAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Telegram> Telegrams { get; set; } = [];
}
