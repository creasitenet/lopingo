using Microsoft.EntityFrameworkCore;
using Lopingo.Data;
using Lopingo.Data.Entities;

namespace Lopingo.Services.Auth;

public sealed class OwnerAuthService
{
    public const int MinPasswordLength = 6;
    private const int BcryptWorkFactor = 12;

    private readonly AppDbContext _db;

    public OwnerAuthService(AppDbContext db) => _db = db;

    public async Task<Owner> SignupAsync(string username, string password, CancellationToken ct = default)
    {
        if (await _db.Owners.AnyAsync(ct))
        {
            throw new InvalidOperationException("An owner account already exists. Use the login page.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }
        username = username.Trim();
        if (username.Length is < 1 or > 64)
        {
            throw new ArgumentException("Username must be between 1 and 64 characters.", nameof(username));
        }

        ValidatePassword(password);
        return await CreateOwnerAsync(username, password, ct);
    }

    public async Task<Owner?> VerifyAsync(string username, string password, CancellationToken ct = default)
    {
        var owner = await _db.Owners.AsNoTracking().FirstOrDefaultAsync(ct);
        if (owner is null) return null;
        if (!string.Equals(owner.Username, username, StringComparison.Ordinal)) return null;
        return BCrypt.Net.BCrypt.Verify(password, owner.PasswordHash) ? owner : null;
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
    {
        ValidatePassword(newPassword);

        var owner = await _db.Owners.FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No owner account to update.");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, owner.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        owner.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BcryptWorkFactor);
        owner.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> OwnerExistsAsync(CancellationToken ct = default)
        => await _db.Owners.AsNoTracking().AnyAsync(ct);

    private async Task<Owner> CreateOwnerAsync(string username, string password, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var owner = new Owner
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, BcryptWorkFactor),
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Owners.Add(owner);
        await _db.SaveChangesAsync(ct);
        return owner;
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
        {
            throw new ArgumentException(
                $"Password must be at least {MinPasswordLength} characters.",
                nameof(password));
        }
    }
}
