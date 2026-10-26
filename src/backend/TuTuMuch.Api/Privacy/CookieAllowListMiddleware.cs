using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace TuTuMuch.Api.Privacy;

/// <summary>The cookies TuTuMuch API may set before consent: Privacy:Cookies:StrictlyNecessary (L2-056).</summary>
public sealed class StrictlyNecessaryCookieOptions
{
    public const string Section = "Privacy:Cookies";

    public string[] StrictlyNecessary { get; set; } = [];
}

/// <summary>
/// Removes, just before headers are sent, every Set-Cookie whose name is not strictly necessary,
/// and logs a warning, so a later change cannot add a non-essential cookie by mistake (L2-056).
/// </summary>
public sealed class CookieAllowListMiddleware(
    RequestDelegate next,
    IOptions<StrictlyNecessaryCookieOptions> options,
    ILogger<CookieAllowListMiddleware> logger)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            var cookies = headers.SetCookie;
            if (cookies.Count == 0)
            {
                return Task.CompletedTask;
            }

            var kept = new List<string>(cookies.Count);
            foreach (var cookie in cookies.OfType<string>())
            {
                var name = cookie[..Math.Max(cookie.IndexOf('=', StringComparison.Ordinal), 0)].Trim();
                if (options.Value.StrictlyNecessary.Contains(name, StringComparer.Ordinal))
                {
                    kept.Add(cookie);
                }
                else
                {
                    logger.LogWarning("Removed cookie {CookieName} set by {Endpoint}: not strictly necessary", name, context.Request.Path);
                }
            }

            headers.SetCookie = new StringValues([.. kept]);
            return Task.CompletedTask;
        });
        return next(context);
    }
}
