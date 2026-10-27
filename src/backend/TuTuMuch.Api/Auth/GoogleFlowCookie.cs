using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TuTuMuch.Api.Auth;

/// <summary>One round trip to Google: what the start endpoint created and the callback must see again (L2-088).</summary>
public sealed record GoogleFlow(string State, string Nonce, string CodeVerifier, string ReturnUrl, DateTime IssuedAt);

/// <summary>
/// Carries the flow between the start endpoint and the callback in the cookie tl_google_flow,
/// signed with the session signing key under its own label so it can be neither forged nor turned
/// into a session token. HttpOnly, Secure and SameSite=Lax (Strict would not come back from
/// Google's cross-site redirect), scoped to the Google endpoints, valid for 10 minutes.
/// </summary>
public sealed class GoogleFlowCookie(IOptions<AuthOptions> options, TimeProvider clock)
{
    public const string Name = "tl_google_flow";
    public const string CookiePath = "/api/auth/google";
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    public void Write(HttpResponse response, GoogleFlow flow)
    {
        var payload = Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(flow));
        response.Cookies.Append(Name, payload + "." + Sign(payload), Options(flow.IssuedAt + Lifetime));
    }

    /// <summary>The flow when the cookie is present, correctly signed and under 10 minutes old; otherwise null.</summary>
    public GoogleFlow? Read(HttpRequest request)
    {
        var value = request.Cookies[Name];
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var dot = value.IndexOf('.', StringComparison.Ordinal);
        if (dot <= 0)
        {
            return null;
        }

        var payload = value[..dot];
        var expected = Encoding.ASCII.GetBytes(Sign(payload));
        var actual = Encoding.ASCII.GetBytes(value[(dot + 1)..]);
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            return null;
        }

        GoogleFlow? flow;
        try
        {
            flow = JsonSerializer.Deserialize<GoogleFlow>(Base64Url.DecodeFromChars(payload));
        }
        catch (Exception exception) when (exception is JsonException or FormatException)
        {
            return null;
        }

        return flow is not null && clock.GetUtcNow().UtcDateTime <= flow.IssuedAt + Lifetime ? flow : null;
    }

    public static void Clear(HttpResponse response) => response.Cookies.Delete(Name, Options(null));

    private string Sign(string payload)
    {
        var key = Encoding.UTF8.GetBytes(options.Value.SigningKey);
        return Base64Url.EncodeToString(HMACSHA256.HashData(key, Encoding.ASCII.GetBytes("google-flow:" + payload)));
    }

    private static CookieOptions Options(DateTime? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = CookiePath,
        Expires = expires,
        IsEssential = true,
    };
}
