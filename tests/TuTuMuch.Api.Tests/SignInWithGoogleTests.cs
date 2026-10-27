// Acceptance Test
// Traces to: L2-088, L2-006, L2-008
// Description: A visitor continues with Google through the server-side code flow: a linked or
// verified member signs in and returns to returnUrl, a new person finishes joining under the L2-001
// rules, an unverified matching email is refused, and cancelled, forged, replayed, unverified and
// unsafe cases create no session and store nothing. Google itself is a fake behind the port.

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using TuTuMuch.Api.Tests.Support;
using TuTuMuch.Application.Abstractions;

namespace TuTuMuch.Api.Tests;

public sealed class SignInWithGoogleTests : IDisposable
{
    private const string ReturnUrl = "/items/L-20931";
    private const string Subject = "google-sub-1";
    private const string Email = "marcus.bell@example.com";

    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _client;

    public SignInWithGoogleTests()
    {
        _client = _factory.CreateMemberClient(followRedirects: false);
    }

    [Fact]
    public async Task Start_redirects_to_google_with_state_nonce_and_pkce_and_a_lax_flow_cookie()
    {
        using var response = await _client.GetAsync($"/api/auth/google/start?returnUrl={Uri.EscapeDataString(ReturnUrl)}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.Equal("accounts.google.com", location.Host);
        var query = QueryHelpers.ParseQuery(location.Query);
        Assert.Equal("test-google-client", query["client_id"].ToString());
        Assert.Equal("https://localhost/api/auth/google/callback", query["redirect_uri"].ToString());
        Assert.Equal("code", query["response_type"].ToString());
        Assert.Equal("openid email", query["scope"].ToString());
        Assert.Equal("S256", query["code_challenge_method"].ToString());
        Assert.False(string.IsNullOrEmpty(query["state"].ToString()));
        Assert.False(string.IsNullOrEmpty(query["nonce"].ToString()));
        Assert.False(string.IsNullOrEmpty(query["code_challenge"].ToString()));
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("tl_google_flow=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth/google", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_new_person_finishes_joining_and_is_a_verified_member_with_no_password()
    {
        var flow = await StartAsync();
        _factory.Google.Identities["code-1"] = new GoogleIdentity(Subject, Email, true, flow.Nonce);

        using var callback = await CallbackAsync(flow.State, "code-1");
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/join/google?returnUrl=%2Fitems%2FL-20931", callback.Headers.Location!.ToString());
        var joinCookie = Assert.Single(callback.Headers.GetValues("Set-Cookie"), c => c.StartsWith("tl_google_join=", StringComparison.Ordinal));
        Assert.Contains("samesite=strict", joinCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", joinCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tl_access=", string.Join(";", callback.Headers.GetValues("Set-Cookie")), StringComparison.Ordinal);

        var pending = await _client.GetFromJsonAsync<PendingResponse>("/api/auth/google/pending");
        Assert.Equal(Email, pending!.Email);

        using var finish = await _client.PostAsJsonAsync("/api/auth/google/finish", new { displayName = "marcus_12", ageConfirmed = true, termsAccepted = true });
        Assert.Equal(HttpStatusCode.OK, finish.StatusCode);
        var cookies = finish.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, c => c.StartsWith("tl_access=", StringComparison.Ordinal));
        Assert.Contains(cookies, c => c.StartsWith("tl_refresh=", StringComparison.Ordinal) && c.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase));

        var me = await _client.GetFromJsonAsync<MeResponse>("/api/auth/me");
        Assert.Equal("marcus_12", me!.DisplayName);
        Assert.Equal(Email, me.Email);
        Assert.True(me.EmailVerified);
        _factory.InDatabase(db =>
        {
            var member = db.Members.Single();
            Assert.Null(member.PasswordHash);
            Assert.NotNull(member.EmailVerifiedAt);
            var login = db.ExternalLogins.Single();
            Assert.Equal(member.Id, login.MemberId);
            Assert.Equal(Subject, login.Subject);
            Assert.NotNull(db.PendingExternalJoins.Single().UsedAt);
        });
        Assert.Empty(_factory.Email.Sent);
    }

    [Fact]
    public async Task A_linked_member_signs_in_and_returns_to_the_return_url()
    {
        await JoinWithGoogleAsync();
        using var signOut = await _client.PostAsync("/api/auth/sign-out", null);
        var flow = await StartAsync();
        _factory.Google.Identities["code-2"] = new GoogleIdentity(Subject, Email, true, flow.Nonce);

        using var callback = await CallbackAsync(flow.State, "code-2");

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal(ReturnUrl, callback.Headers.Location!.ToString());
        Assert.Contains(callback.Headers.GetValues("Set-Cookie"), c => c.StartsWith("tl_access=", StringComparison.Ordinal));
        using var me = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        _factory.InDatabase(db => Assert.Equal(1, db.Members.Count()));
    }

    [Fact]
    public async Task A_verified_member_with_the_same_email_is_linked_and_signed_in()
    {
        await _client.RegisterAsync();
        await _client.VerifyAsync(_factory);
        var flow = await StartAsync();
        _factory.Google.Identities["code-3"] = new GoogleIdentity(Subject, "Marcus.Bell@Example.com", true, flow.Nonce);

        using var callback = await CallbackAsync(flow.State, "code-3");

        Assert.Equal(ReturnUrl, callback.Headers.Location!.ToString());
        using var me = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        _factory.InDatabase(db =>
        {
            var login = db.ExternalLogins.Single();
            Assert.Equal(db.Members.Single().Id, login.MemberId);
            Assert.Equal(Subject, login.Subject);
        });
    }

    [Fact]
    public async Task An_unverified_member_with_the_same_email_is_refused_so_the_account_cannot_be_taken_over()
    {
        await _client.RegisterAsync();
        var flow = await StartAsync();
        _factory.Google.Identities["code-4"] = new GoogleIdentity(Subject, Email, true, flow.Nonce);

        using var callback = await CallbackAsync(flow.State, "code-4");

        Assert.Equal("/sign-in?google=existing", callback.Headers.Location!.ToString());
        AssertNoSession(callback);
        _factory.InDatabase(db =>
        {
            Assert.Empty(db.ExternalLogins);
            Assert.Empty(db.PendingExternalJoins);
        });
    }

    [Fact]
    public async Task A_closed_member_is_refused()
    {
        await _client.RegisterAsync();
        await _client.VerifyAsync(_factory);
        _factory.InDatabase(db =>
        {
            db.Members.Single().Close(DateTime.UtcNow);
            db.SaveChanges();
        });
        var flow = await StartAsync();
        _factory.Google.Identities["code-5"] = new GoogleIdentity(Subject, Email, true, flow.Nonce);

        using var callback = await CallbackAsync(flow.State, "code-5");

        Assert.StartsWith("/sign-in?google=failed&ref=", callback.Headers.Location!.ToString(), StringComparison.Ordinal);
        AssertNoSession(callback);
    }

    [Fact]
    public async Task Finish_reports_every_field_error_of_registration()
    {
        await _client.RegisterAsync("tutu_tessa", "tessa@example.com");
        await ReachFinishAsync();

        using var finish = await _client.PostAsJsonAsync("/api/auth/google/finish", new { displayName = "TUTU_TESSA", ageConfirmed = false, termsAccepted = false });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, finish.StatusCode);
        var problem = await Problem.ReadAsync(finish);
        Assert.Equal("That display name is taken", problem.Errors!["displayName"].Message);
        Assert.Equal("Confirm you're 18 or older", problem.Errors["ageConfirmed"].Message);
        Assert.Equal("Accept the terms to continue", problem.Errors["termsAccepted"].Message);
        _factory.InDatabase(db => Assert.Equal(1, db.Members.Count()));

        using var badFormat = await _client.PostAsJsonAsync("/api/auth/google/finish", new { displayName = "a b", ageConfirmed = true, termsAccepted = true });
        Assert.Equal("Use 3 to 30 letters, numbers, _ or -", (await Problem.ReadAsync(badFormat)).Errors!["displayName"].Message);
    }

    [Fact]
    public async Task A_pending_join_is_single_use()
    {
        await JoinWithGoogleAsync();

        using var again = await _client.PostAsJsonAsync("/api/auth/google/finish", new { displayName = "marcus_13", ageConfirmed = true, termsAccepted = true });

        Assert.Equal(HttpStatusCode.Gone, again.StatusCode);
        Assert.Equal("GOOGLE_JOIN_EXPIRED", (await Problem.ReadAsync(again)).Code);
        _factory.InDatabase(db => Assert.Equal(1, db.Members.Count()));
    }

    [Fact]
    public async Task A_pending_join_expires_after_10_minutes()
    {
        await ReachFinishAsync();
        _factory.Clock.Advance(TimeSpan.FromMinutes(11));

        using var pending = await _client.GetAsync("/api/auth/google/pending");
        using var finish = await _client.PostAsJsonAsync("/api/auth/google/finish", new { displayName = "marcus_12", ageConfirmed = true, termsAccepted = true });

        Assert.Equal(HttpStatusCode.Gone, pending.StatusCode);
        Assert.Equal(HttpStatusCode.Gone, finish.StatusCode);
        _factory.InDatabase(db => Assert.Empty(db.Members));
    }

    [Fact]
    public async Task Cancelling_at_google_returns_to_sign_in_with_no_session()
    {
        var flow = await StartAsync();

        using var callback = await _client.GetAsync($"/api/auth/google/callback?state={Uri.EscapeDataString(flow.State)}&error=access_denied");

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/sign-in?google=cancelled", callback.Headers.Location!.ToString());
        AssertNoSession(callback);
        Assert.Empty(_factory.Google.Exchanges);
    }

    [Fact]
    public async Task A_forged_state_a_refused_code_a_wrong_nonce_and_a_replay_are_refused()
    {
        var flow = await StartAsync();
        _factory.Google.Identities["code-6"] = new GoogleIdentity(Subject, Email, true, flow.Nonce);

        using var forged = await CallbackAsync("not-the-state", "code-6");
        Assert.StartsWith("/sign-in?google=failed&ref=", forged.Headers.Location!.ToString(), StringComparison.Ordinal);
        AssertNoSession(forged);
        Assert.Empty(_factory.Google.Exchanges);

        flow = await StartAsync();
        using var refused = await CallbackAsync(flow.State, "code-google-never-issued");
        Assert.StartsWith("/sign-in?google=failed&ref=", refused.Headers.Location!.ToString(), StringComparison.Ordinal);
        AssertNoSession(refused);

        flow = await StartAsync();
        _factory.Google.Identities["code-7"] = new GoogleIdentity(Subject, Email, true, "another-nonce");
        using var wrongNonce = await CallbackAsync(flow.State, "code-7");
        Assert.StartsWith("/sign-in?google=failed&ref=", wrongNonce.Headers.Location!.ToString(), StringComparison.Ordinal);
        AssertNoSession(wrongNonce);

        flow = await StartAsync();
        _factory.Google.Identities["code-8"] = new GoogleIdentity(Subject, Email, true, flow.Nonce);
        using var accepted = await CallbackAsync(flow.State, "code-8");
        Assert.StartsWith("/join/google", accepted.Headers.Location!.ToString(), StringComparison.Ordinal);
        using var replayed = await CallbackAsync(flow.State, "code-8");
        Assert.StartsWith("/sign-in?google=failed&ref=", replayed.Headers.Location!.ToString(), StringComparison.Ordinal);
        _factory.InDatabase(db =>
        {
            Assert.Empty(db.Members);
            Assert.Empty(db.ExternalLogins);
            Assert.Equal(1, db.PendingExternalJoins.Count());
        });
    }

    [Fact]
    public async Task A_google_email_that_google_has_not_verified_is_refused_and_nothing_is_stored()
    {
        var flow = await StartAsync();
        _factory.Google.Identities["code-9"] = new GoogleIdentity(Subject, Email, false, flow.Nonce);

        using var callback = await CallbackAsync(flow.State, "code-9");

        Assert.StartsWith("/sign-in?google=failed&ref=", callback.Headers.Location!.ToString(), StringComparison.Ordinal);
        AssertNoSession(callback);
        _factory.InDatabase(db =>
        {
            Assert.Empty(db.Members);
            Assert.Empty(db.ExternalLogins);
            Assert.Empty(db.PendingExternalJoins);
        });
    }

    [Theory]
    [InlineData("https://evil.example/")]
    [InlineData("//evil.example/")]
    [InlineData("javascript:alert(1)")]
    [InlineData("")]
    public async Task An_unsafe_return_url_falls_back_to_the_home_page(string returnUrl)
    {
        await _client.RegisterAsync();
        await _client.VerifyAsync(_factory);
        var flow = await StartAsync(returnUrl);
        _factory.Google.Identities["code-10"] = new GoogleIdentity(Subject, Email, true, flow.Nonce);

        using var callback = await CallbackAsync(flow.State, "code-10");

        Assert.Equal("/", callback.Headers.Location!.ToString());
    }

    [Fact]
    public async Task A_google_only_member_gets_the_usual_answer_to_a_password_sign_in()
    {
        await JoinWithGoogleAsync();

        using var response = await _client.SignInAsync(Email, "any password at all");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Email or password is incorrect", (await Problem.ReadAsync(response)).Title);
    }

    [Fact]
    public async Task Providers_say_whether_google_is_configured()
    {
        var configured = await _client.GetFromJsonAsync<ProvidersResponse>("/api/auth/providers");
        Assert.True(configured!.Google);

        _factory.Google.ClientId = null;
        var off = await _client.GetFromJsonAsync<ProvidersResponse>("/api/auth/providers");
        Assert.False(off!.Google);
    }

    [Fact]
    public async Task Every_google_endpoint_is_404_without_a_client()
    {
        _factory.Google.ClientId = null;

        using var start = await _client.GetAsync("/api/auth/google/start");
        using var callback = await _client.GetAsync("/api/auth/google/callback?state=x&code=y");
        using var pending = await _client.GetAsync("/api/auth/google/pending");
        using var finish = await _client.PostAsJsonAsync("/api/auth/google/finish", new { displayName = "marcus_12", ageConfirmed = true, termsAccepted = true });

        Assert.All(new[] { start, callback, pending, finish }, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
        Assert.Equal("GOOGLE_NOT_CONFIGURED", (await Problem.ReadAsync(start)).Code);
    }

    [Fact]
    public async Task No_code_or_token_appears_in_any_response()
    {
        var flow = await StartAsync();
        _factory.Google.Identities["code-11"] = new GoogleIdentity(Subject, Email, true, flow.Nonce);

        using var callback = await CallbackAsync(flow.State, "code-11");
        var joinToken = callback.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("tl_google_join=", StringComparison.Ordinal));
        joinToken = joinToken["tl_google_join=".Length..joinToken.IndexOf(';', StringComparison.Ordinal)];
        var location = callback.Headers.Location!.ToString();
        Assert.DoesNotContain(joinToken, location, StringComparison.Ordinal);
        Assert.DoesNotContain("code-11", location, StringComparison.Ordinal);

        using var finish = await _client.PostAsJsonAsync("/api/auth/google/finish", new { displayName = "marcus_12", ageConfirmed = true, termsAccepted = true });
        var body = await finish.Content.ReadAsStringAsync();
        Assert.DoesNotContain(joinToken, body, StringComparison.Ordinal);
        Assert.DoesNotContain("token", body, StringComparison.OrdinalIgnoreCase);
        var exchange = Assert.Single(_factory.Google.Exchanges);
        Assert.Equal("https://localhost/api/auth/google/callback", exchange.RedirectUri);
        Assert.False(string.IsNullOrEmpty(exchange.CodeVerifier));
    }

    private async Task<(string State, string Nonce)> StartAsync(string returnUrl = ReturnUrl)
    {
        using var response = await _client.GetAsync($"/api/auth/google/start?returnUrl={Uri.EscapeDataString(returnUrl)}");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var query = QueryHelpers.ParseQuery(response.Headers.Location!.Query);
        return (query["state"].ToString(), query["nonce"].ToString());
    }

    private Task<HttpResponseMessage> CallbackAsync(string state, string code) =>
        _client.GetAsync($"/api/auth/google/callback?state={Uri.EscapeDataString(state)}&code={Uri.EscapeDataString(code)}");

    /// <summary>Runs the flow for a new person up to the finish-joining page.</summary>
    private async Task ReachFinishAsync()
    {
        var flow = await StartAsync();
        _factory.Google.Identities["code-pending"] = new GoogleIdentity(Subject, Email, true, flow.Nonce);
        using var callback = await CallbackAsync(flow.State, "code-pending");
        Assert.StartsWith("/join/google", callback.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    /// <summary>A new person joins with Google and is signed in.</summary>
    private async Task JoinWithGoogleAsync()
    {
        await ReachFinishAsync();
        using var finish = await _client.PostAsJsonAsync("/api/auth/google/finish", new { displayName = "marcus_12", ageConfirmed = true, termsAccepted = true });
        finish.EnsureSuccessStatusCode();
    }

    private static void AssertNoSession(HttpResponseMessage response)
    {
        var cookies = response.Headers.TryGetValues("Set-Cookie", out var values) ? string.Join(";", values) : string.Empty;
        Assert.DoesNotContain("tl_access=", cookies, StringComparison.Ordinal);
        Assert.DoesNotContain("tl_refresh=", cookies, StringComparison.Ordinal);
    }

    private sealed record PendingResponse(string Email);

    private sealed record MeResponse(string DisplayName, string Email, bool EmailVerified);

    private sealed record ProvidersResponse(bool Google);

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
