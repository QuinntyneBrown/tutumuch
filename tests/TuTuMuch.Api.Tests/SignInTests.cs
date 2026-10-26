// Acceptance Test
// Traces to: L2-006, L2-008
// Description: Members sign in with email and password into HttpOnly, Secure, SameSite=Strict
// session cookies; the refresh token rotates, reuse revokes the family and signing out revokes it.

using System.Net;
using System.Net.Http.Json;
using TuTuMuch.Api.Tests.Support;

namespace TuTuMuch.Api.Tests;

public sealed class SignInTests : IDisposable
{
    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _client;

    public SignInTests()
    {
        _client = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task Sign_in_sets_strict_http_only_secure_session_cookies()
    {
        await _client.RegisterAsync();
        await _client.VerifyAsync(_factory);

        using var response = await _client.SignInAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, c => c.StartsWith("tl_access=", StringComparison.Ordinal));
        Assert.Contains(cookies, c => c.StartsWith("tl_refresh=", StringComparison.Ordinal));
        Assert.All(cookies, c =>
        {
            Assert.Contains("httponly", c, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("secure", c, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=strict", c, StringComparison.OrdinalIgnoreCase);
        });
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("token", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Current_member_is_returned_after_sign_in()
    {
        await _client.SignInVerifiedMemberAsync(_factory);

        var me = await _client.GetFromJsonAsync<MeResponse>("/api/auth/me");

        Assert.Equal("marcus_12", me!.DisplayName);
        Assert.Equal("marcus.bell@example.com", me.Email);
        Assert.True(me.EmailVerified);
    }

    [Fact]
    public async Task Current_member_is_401_for_a_visitor()
    {
        using var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_email_and_wrong_password_get_the_same_answer()
    {
        await _client.RegisterAsync();

        using var unknown = await _client.SignInAsync("nobody@example.com");
        using var wrong = await _client.SignInAsync(password: "not the right password");

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(unknown.StatusCode, wrong.StatusCode);
        Assert.Equal("Email or password is incorrect", (await Problem.ReadAsync(unknown)).Title);
        Assert.Equal("Email or password is incorrect", (await Problem.ReadAsync(wrong)).Title);
    }

    [Fact]
    public async Task Email_capitalisation_does_not_matter_at_sign_in()
    {
        await _client.RegisterAsync();

        using var response = await _client.SignInAsync("MARCUS.Bell@Example.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_reuse_revokes_the_family()
    {
        await _client.RegisterAsync();
        using var signIn = await _client.SignInAsync();
        var original = RefreshCookie(signIn);

        using var rotated = await _client.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        Assert.NotEqual(original, RefreshCookie(rotated));

        using var reused = await ReplayRefreshAsync(original);
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);

        using var afterReuse = await _client.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task Signing_out_revokes_the_refresh_token()
    {
        await _client.RegisterAsync();
        using var signIn = await _client.SignInAsync();
        var refresh = RefreshCookie(signIn);

        using var signOut = await _client.PostAsync("/api/auth/sign-out", null);

        Assert.Equal(HttpStatusCode.NoContent, signOut.StatusCode);
        using var refused = await ReplayRefreshAsync(refresh);
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
    }

    [Fact]
    public async Task Access_token_expires_after_15_minutes_and_refresh_renews_it()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        _factory.Clock.Advance(TimeSpan.FromMinutes(16));

        using var expired = await _client.GetAsync("/api/auth/me");
        using var refreshed = await _client.PostAsync("/api/auth/refresh", null);
        using var renewed = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
    }

    private async Task<HttpResponseMessage> ReplayRefreshAsync(string refreshToken)
    {
        using var anonymous = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var replay = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        replay.Headers.Add("Cookie", $"tl_refresh={refreshToken}");
        return await anonymous.SendAsync(replay);
    }

    private static string RefreshCookie(HttpResponseMessage response)
    {
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("tl_refresh=", StringComparison.Ordinal));
        return cookie["tl_refresh=".Length..cookie.IndexOf(';', StringComparison.Ordinal)];
    }

    private sealed record MeResponse(string DisplayName, string Email, bool EmailVerified);

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
