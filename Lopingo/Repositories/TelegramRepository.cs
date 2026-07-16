using Microsoft.EntityFrameworkCore;
using Lopingo.Data;
using Lopingo.Data.Entities;

namespace Lopingo.Repositories;

public sealed class TelegramRepository
{
    private readonly AppDbContext _db;

    public TelegramRepository(AppDbContext db) => _db = db;

    public Task<List<Telegram>> ListAsync(CancellationToken ct = default)
        => _db.Telegrams
            .Include(t => t.Monitors)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public Task<List<Telegram>> ListEnabledForMonitorAsync(Guid monitorId, CancellationToken ct = default)
        => _db.Monitors
            .Where(m => m.Id == monitorId)
            .SelectMany(m => m.Telegrams)
            .Where(t => t.Enabled)
            .ToListAsync(ct);

    public Task<Telegram?> GetAsync(Guid id, CancellationToken ct = default)
        => _db.Telegrams.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Telegram> CreateAsync(
        string name, string botToken, string chatId, bool enabled, CancellationToken ct = default)
    {
        Validate(name, botToken, chatId);
        var now = DateTime.UtcNow;
        var telegram = new Telegram
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            BotToken = botToken.Trim(),
            ChatId = chatId.Trim(),
            Enabled = enabled,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Telegrams.Add(telegram);
        await _db.SaveChangesAsync(ct);
        return telegram;
    }

    public async Task<bool> UpdateAsync(
        Guid id, string name, string? botToken, string chatId, bool enabled, CancellationToken ct = default)
    {
        var telegram = await GetAsync(id, ct);
        if (telegram is null) return false;

        name = name.Trim();
        chatId = chatId.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(chatId))
            throw new ArgumentException("Chat ID is required.", nameof(chatId));

        telegram.Name = name;
        telegram.ChatId = chatId;
        telegram.Enabled = enabled;
        if (!string.IsNullOrWhiteSpace(botToken))
            telegram.BotToken = botToken.Trim();
        telegram.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var telegram = await GetAsync(id, ct);
        if (telegram is null) return false;
        _db.Telegrams.Remove(telegram);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static void Validate(string name, string botToken, string chatId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(botToken))
            throw new ArgumentException("Bot token is required.", nameof(botToken));
        if (string.IsNullOrWhiteSpace(chatId))
            throw new ArgumentException("Chat ID is required.", nameof(chatId));
    }
}
