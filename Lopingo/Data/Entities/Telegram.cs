using System.ComponentModel.DataAnnotations;

namespace Lopingo.Data.Entities;

public sealed class Telegram
{
    public Guid Id { get; set; }

    [Required, MaxLength(64)]
    public string Name { get; set; } = "";

    [Required, MaxLength(128)]
    public string BotToken { get; set; } = "";

    [Required, MaxLength(64)]
    public string ChatId { get; set; } = "";

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Monitor> Monitors { get; set; } = [];
}
