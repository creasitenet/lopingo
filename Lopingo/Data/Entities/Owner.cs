using System.ComponentModel.DataAnnotations;

namespace Lopingo.Data.Entities;

public sealed class Owner
{
    public int Id { get; set; } = 1;

    [Required, MaxLength(64)]
    public string Username { get; set; } = "";

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
