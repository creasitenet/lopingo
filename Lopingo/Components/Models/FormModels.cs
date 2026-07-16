using System.ComponentModel.DataAnnotations;

namespace Lopingo.Components.Models;

public sealed class LoginFormModel
{
    [Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = "";

    public bool Remember { get; set; } = true;

    public string? ReturnUrl { get; set; }
}

public sealed class SignupFormModel
{
    [Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please retype your password.")]
    public string Confirm { get; set; } = "";
}

public sealed class PasswordChangeFormModel
{
    [Required(ErrorMessage = "Current password is required.")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "New password is required.")]
    public string NewPassword { get; set; } = "";
}

public sealed class MonitorFormModel
{
    [Required(ErrorMessage = "URL is required.")]
    public string Url { get; set; } = "";

    public int FreqSec { get; set; } = 60;
}

public sealed class TelegramFormModel
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(64)]
    public string Name { get; set; } = "";

    public string? BotToken { get; set; }

    [Required(ErrorMessage = "Chat ID is required.")]
    [MaxLength(64)]
    public string ChatId { get; set; } = "";

    public bool Enabled { get; set; } = true;
}
