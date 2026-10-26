using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TutuMuch.Application.Accounts;

namespace TutuMuch.Api.Auth;

/// <summary>Session settings (L2-008). The signing key comes from configuration, never source.</summary>
public sealed class AuthOptions
{
    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "Tutu Much";

    public SymmetricSecurityKey Key => new(Encoding.UTF8.GetBytes(SigningKey));
}

/// <summary>
/// Issues the 15-minute access token and writes both session cookies as HttpOnly, Secure and
/// SameSite=Strict, so no token ever reaches web storage (L2-008).
/// </summary>
public sealed class SessionCookies(IOptions<AuthOptions> options, TimeProvider clock)
{
    public const string AccessCookie = "tm_access";
    public const string RefreshCookie = "tm_refresh";
    public const string RefreshPath = "/api/auth";
    public static readonly TimeSpan AccessLifetime = TimeSpan.FromMinutes(15);

    public void Write(HttpResponse response, SignedIn session)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Value.Issuer,
            Audience = options.Value.Issuer,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, session.MemberId.ToString())]),
            IssuedAt = now,
            NotBefore = now,
            Expires = now + AccessLifetime,
            SigningCredentials = new SigningCredentials(options.Value.Key, SecurityAlgorithms.HmacSha256),
        });
        response.Cookies.Append(AccessCookie, token, Cookie("/", now + AccessLifetime));
        response.Cookies.Append(RefreshCookie, session.RefreshToken, Cookie(RefreshPath, session.RefreshExpiresAt));
    }

    public static void Clear(HttpResponse response)
    {
        response.Cookies.Delete(AccessCookie, Cookie("/", null));
        response.Cookies.Delete(RefreshCookie, Cookie(RefreshPath, null));
    }

    private static CookieOptions Cookie(string path, DateTime? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = path,
        Expires = expires,
        IsEssential = true,
    };
}

public static class ClaimsPrincipalExtensions
{
    public static Guid MemberId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
