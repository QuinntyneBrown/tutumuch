// Acceptance Test
// Traces to: L2-056
// Description: Before any consent, Tutu Much API sets only strictly necessary cookies. Every Set-Cookie
// on a sample of endpoints, signed in and signed out, is named in Privacy:Cookies:StrictlyNecessary,
// and a cookie outside that list is removed before the response leaves the API.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TutuMuch.Api.Tests.Support;

namespace TutuMuch.Api.Tests;

public sealed class CookieConsentTests : IDisposable
{
    private static readonly string[] StrictlyNecessary = ["tm_access", "tm_refresh"];

    private readonly TutuMuchApiFactory _factory = new();

    [Fact]
    public async Task Signed_out_and_signed_in_endpoints_set_only_strictly_necessary_cookies()
    {
        var client = _factory.CreateMemberClient();
        var names = new List<string>();

        names.AddRange(await CookieNamesAsync(await client.GetAsync("/api/catalog/listings")));
        names.AddRange(await CookieNamesAsync(await client.GetAsync("/api/auth/me")));
        await client.RegisterAsync();
        await client.VerifyAsync(_factory);
        names.AddRange(await CookieNamesAsync(await client.SignInAsync()));
        names.AddRange(await CookieNamesAsync(await client.PostAsync("/api/auth/refresh", null)));
        names.AddRange(await CookieNamesAsync(await client.GetAsync("/api/me/notification-preferences")));
        names.AddRange(await CookieNamesAsync(await client.PostAsync("/api/auth/sign-out", null)));

        Assert.NotEmpty(names);
        Assert.All(names, name => Assert.Contains(name, StrictlyNecessary));
    }

    [Fact]
    public async Task A_cookie_outside_the_strictly_necessary_list_is_removed()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddTransient<Microsoft.AspNetCore.Hosting.IStartupFilter, StrayCookieEndpoint>()));
        _factory.EnsureDatabase();
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        using var response = await client.GetAsync("/test/stray-cookie");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var names = await CookieNamesAsync(response);
        Assert.Contains("tm_access", names);
        Assert.DoesNotContain("tracker", names);
    }

    private static Task<List<string>> CookieNamesAsync(HttpResponseMessage response)
    {
        using (response)
        {
            var names = response.Headers.TryGetValues("Set-Cookie", out var values)
                ? values.Select(value => value[..value.IndexOf('=', StringComparison.Ordinal)]).ToList()
                : [];
            return Task.FromResult(names);
        }
    }

    /// <summary>A route after the app's own pipeline that sets one necessary and one stray cookie.</summary>
    private sealed class StrayCookieEndpoint : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Run(async context =>
            {
                if (context.Request.Path != "/test/stray-cookie")
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                context.Response.Cookies.Append("tm_access", "x", new CookieOptions { Secure = true, HttpOnly = true });
                context.Response.Cookies.Append("tracker", "x", new CookieOptions { Secure = true });
                await context.Response.WriteAsync("ok");
            });
        };
    }

    public void Dispose() => _factory.Dispose();
}
