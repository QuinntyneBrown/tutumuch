using System.Net.Http.Json;

namespace TuTuMuch.Api.Tests.Support;

/// <summary>Drives the accounts endpoints to put a member in a known state.</summary>
public static class Members
{
    public const string Password = "correct horse battery";

    public static object Registration(string displayName = "marcus_12", string email = "marcus.bell@example.com", string password = Password) =>
        new { displayName, email, password, ageConfirmed = true, termsAccepted = true };

    public static async Task RegisterAsync(this HttpClient client, string displayName = "marcus_12", string email = "marcus.bell@example.com")
    {
        using var response = await client.PostAsJsonAsync("/api/auth/register", Registration(displayName, email));
        response.EnsureSuccessStatusCode();
    }

    public static async Task VerifyAsync(this HttpClient client, TuTuMuchApiFactory factory, string email = "marcus.bell@example.com")
    {
        await factory.DispatchEmailsAsync();
        using var response = await client.PostAsJsonAsync("/api/auth/verify-email", new { token = factory.Email.LastVerificationToken(email) });
        response.EnsureSuccessStatusCode();
    }

    public static async Task<HttpResponseMessage> SignInAsync(this HttpClient client, string email = "marcus.bell@example.com", string password = Password) =>
        await client.PostAsJsonAsync("/api/auth/sign-in", new { email, password });

    /// <summary>Registers, verifies and signs in a member on <paramref name="client"/>.</summary>
    public static async Task SignInVerifiedMemberAsync(this HttpClient client, TuTuMuchApiFactory factory, string displayName = "marcus_12", string email = "marcus.bell@example.com")
    {
        await client.RegisterAsync(displayName, email);
        await client.VerifyAsync(factory, email);
        using var response = await client.SignInAsync(email);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Registers and signs in a member who has not verified their email.</summary>
    public static async Task SignInUnverifiedMemberAsync(this HttpClient client, string displayName = "marcus_12", string email = "marcus.bell@example.com")
    {
        await client.RegisterAsync(displayName, email);
        using var response = await client.SignInAsync(email);
        response.EnsureSuccessStatusCode();
    }
}
