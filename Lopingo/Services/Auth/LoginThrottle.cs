using Microsoft.Extensions.Caching.Memory;

namespace Lopingo.Services.Auth;

public sealed class LoginThrottle
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;

    public LoginThrottle(IMemoryCache cache) => _cache = cache;

    public bool IsBlocked(string key) =>
        _cache.TryGetValue(AttemptsKey(key), out int count) && count >= MaxAttempts;

    public void RecordFailure(string key)
    {
        var attempts = _cache.GetOrCreate(AttemptsKey(key), e =>
        {
            e.AbsoluteExpirationRelativeToNow = Window;
            return 0;
        });
        _cache.Set(AttemptsKey(key), attempts + 1, Window);
    }

    public void Reset(string key) => _cache.Remove(AttemptsKey(key));

    private static string AttemptsKey(string key) => $"login:{key}";
}
