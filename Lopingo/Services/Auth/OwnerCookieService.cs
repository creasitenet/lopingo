using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Lopingo.Services.Auth;

public sealed class OwnerCookieService
{
    public const string AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string OwnerIdClaim = "owner_id";

    private readonly IHttpContextAccessor _http;

    public OwnerCookieService(IHttpContextAccessor http) => _http = http;

    public Task SignInAsync(HttpContext httpContext, string username, int ownerId, bool isPersistent)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(OwnerIdClaim, ownerId.ToString()),
        };
        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        return httpContext.SignInAsync(
            AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = isPersistent });
    }

    public Task SignOutAsync()
    {
        var http = _http.HttpContext
            ?? throw new InvalidOperationException("No active HTTP request.");
        return http.SignOutAsync(AuthenticationScheme);
    }

    public int? GetOwnerId()
    {
        var raw = _http.HttpContext?.User.FindFirst(OwnerIdClaim)?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }
}
