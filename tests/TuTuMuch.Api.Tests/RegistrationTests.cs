// Acceptance Test
// Traces to: L2-001, L2-002
// Description: A visitor registers with a display name, email and password; the account starts
// unverified and a single-use verification link valid for 24 hours is emailed.

using System.Net;
using System.Net.Http.Json;
using TuTuMuch.Api.Tests.Support;

namespace TuTuMuch.Api.Tests;

public sealed class RegistrationTests : IDisposable
{
    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _client;

    public RegistrationTests()
    {
        _client = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task Register_creates_an_unverified_member_and_emails_a_link()
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/register", Members.Registration());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _factory.InDatabase(db =>
        {
            var member = db.Members.Single();
            Assert.Equal("marcus_12", member.DisplayName);
            Assert.Null(member.EmailVerifiedAt);
            Assert.NotEqual(Members.Password, member.PasswordHash);
        });
        var email = Assert.Single(await _factory.DispatchEmailsAsync());
        Assert.Equal("marcus.bell@example.com", email.To);
        Assert.Contains("https://tutumuch.test/verify?token=", email.Body);
    }

    [Fact]
    public async Task Register_rejects_an_email_that_exists_ignoring_case()
    {
        await _client.RegisterAsync(email: "Sam@Example.com");

        using var response = await _client.PostAsJsonAsync("/api/auth/register", Members.Registration("sam_two", "sam@example.com"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("An account with this email already exists", problem.Errors!["email"].Message);
        _factory.InDatabase(db => Assert.Equal(1, db.Members.Count()));
    }

    [Fact]
    public async Task Register_rejects_a_display_name_that_is_taken_ignoring_case()
    {
        await _client.RegisterAsync("tutu_tessa", "tessa@example.com");

        using var response = await _client.PostAsJsonAsync("/api/auth/register", Members.Registration("TUTU_TESSA", "other@example.com"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("That display name is taken", (await Problem.ReadAsync(response)).Errors!["displayName"].Message);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("abcdefghijklmnopqrstuvwxyz12345")]
    [InlineData("has space")]
    public async Task Register_rejects_a_badly_formed_display_name(string displayName)
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/register", Members.Registration(displayName));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("DISPLAY_NAME_FORMAT", (await Problem.ReadAsync(response)).Errors!["displayName"].Code);
    }

    [Fact]
    public async Task Register_rejects_a_password_shorter_than_12_characters()
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/register", Members.Registration(password: "elevenchars"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("Password must be at least 12 characters", (await Problem.ReadAsync(response)).Errors!["password"].Message);
    }

    [Fact]
    public async Task Register_rejects_a_breached_password()
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/register", Members.Registration(password: "password1234"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("This password has appeared in a data breach. Choose another.", (await Problem.ReadAsync(response)).Errors!["password"].Message);
    }

    [Fact]
    public async Task Register_requires_the_age_and_terms_ticks()
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { displayName = "marcus_12", email = "marcus.bell@example.com", password = Members.Password, ageConfirmed = false, termsAccepted = false });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("Confirm you're 18 or older", problem.Errors!["ageConfirmed"].Message);
        Assert.Equal("Accept the terms to continue", problem.Errors!["termsAccepted"].Message);
        _factory.InDatabase(db => Assert.False(db.Members.Any()));
    }

    [Fact]
    public async Task Verification_link_marks_the_member_verified_once()
    {
        await _client.RegisterAsync();
        await _factory.DispatchEmailsAsync();
        var token = _factory.Email.LastVerificationToken("marcus.bell@example.com");

        using var first = await _client.PostAsJsonAsync("/api/auth/verify-email", new { token });
        using var second = await _client.PostAsJsonAsync("/api/auth/verify-email", new { token });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Equal("VERIFICATION_LINK_USED", (await Problem.ReadAsync(second)).Code);
        _factory.InDatabase(db => Assert.NotNull(db.Members.Single().EmailVerifiedAt));
    }

    [Fact]
    public async Task Verification_link_older_than_24_hours_has_expired()
    {
        await _client.RegisterAsync();
        await _factory.DispatchEmailsAsync();
        var token = _factory.Email.LastVerificationToken("marcus.bell@example.com");
        _factory.Clock.Advance(TimeSpan.FromHours(24) + TimeSpan.FromSeconds(1));

        using var response = await _client.PostAsJsonAsync("/api/auth/verify-email", new { token });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("VERIFICATION_LINK_EXPIRED", problem.Code);
        Assert.Equal("This link has expired", problem.Title);
        _factory.InDatabase(db => Assert.Null(db.Members.Single().EmailVerifiedAt));
    }

    [Fact]
    public async Task Requesting_a_new_link_stops_earlier_links_working()
    {
        await _client.RegisterAsync();
        await _factory.DispatchEmailsAsync();
        var first = _factory.Email.LastVerificationToken("marcus.bell@example.com");

        using var resend = await _client.PostAsJsonAsync("/api/auth/verification-email", new { email = "marcus.bell@example.com" });
        using var old = await _client.PostAsJsonAsync("/api/auth/verify-email", new { token = first });

        Assert.Equal(HttpStatusCode.Accepted, resend.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, old.StatusCode);
        await _client.VerifyAsync(_factory);
    }

    [Fact]
    public async Task A_fourth_link_request_within_an_hour_is_refused()
    {
        await _client.RegisterAsync();
        for (var i = 0; i < 2; i++)
        {
            using var ok = await _client.PostAsJsonAsync("/api/auth/verification-email", new { email = "marcus.bell@example.com" });
            Assert.Equal(HttpStatusCode.Accepted, ok.StatusCode);
        }

        using var refused = await _client.PostAsJsonAsync("/api/auth/verification-email", new { email = "marcus.bell@example.com" });

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.True(refused.Headers.Contains("Retry-After"));
        _factory.InDatabase(db => Assert.Equal(3, db.EmailVerificationTokens.Count()));
    }

    [Fact]
    public async Task Requesting_a_link_for_an_unknown_email_reveals_nothing()
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/verification-email", new { email = "nobody@example.com" });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(await _factory.DispatchEmailsAsync());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
