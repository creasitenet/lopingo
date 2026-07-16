using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Lopingo.Services.Notifications;

public interface ITelegramNotifier
{
    Task SendAsync(string botToken, string chatId, string message, CancellationToken ct = default);
}

public sealed class TelegramNotifier(HttpClient http, ILogger<TelegramNotifier> log) : ITelegramNotifier
{
    public async Task SendAsync(string botToken, string chatId, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(botToken))
            throw new ArgumentException("Telegram bot token is required.", nameof(botToken));
        if (string.IsNullOrWhiteSpace(chatId))
            throw new ArgumentException("Telegram chat ID is required.", nameof(chatId));

        var url = $"https://api.telegram.org/bot{botToken.Trim()}/sendMessage";
        using var response = await http.PostAsJsonAsync(url, new TelegramSendRequest(chatId.Trim(), message), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            log.LogWarning("Telegram API returned {Status}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException(ParseTelegramError(body) ?? $"Telegram API error ({(int)response.StatusCode}).");
        }

        var parsed = System.Text.Json.JsonSerializer.Deserialize<TelegramApiResponse>(body);
        if (parsed is { Ok: false })
            throw new InvalidOperationException(parsed.Description ?? "Telegram API rejected the message.");
    }

    private static string? ParseTelegramError(string body)
    {
        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<TelegramApiResponse>(body);
            return parsed?.Description;
        }
        catch
        {
            return null;
        }
    }

    private sealed record TelegramSendRequest(
        [property: JsonPropertyName("chat_id")] string ChatId,
        [property: JsonPropertyName("text")] string Text);

    private sealed class TelegramApiResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
