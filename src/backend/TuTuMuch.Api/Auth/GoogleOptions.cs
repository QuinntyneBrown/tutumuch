namespace TuTuMuch.Api.Auth;

/// <summary>
/// The Google OAuth client (Auth:Google), from the environment only (L2-085, L2-088). Both the ID
/// and the secret, or neither: without them the feature is off and every Google endpoint answers 404.
/// </summary>
public sealed class GoogleOptions
{
    public const string Section = "Auth:Google";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Overrides the redirect URI built from the request, for example behind a proxy that hides the host.</summary>
    public string? RedirectUri { get; set; }

    public bool IsConfigured => !string.IsNullOrEmpty(ClientId);

    /// <summary>Set either both values or none, so a half-configured client fails at start-up rather than at the first sign-in.</summary>
    public bool IsConsistent => string.IsNullOrEmpty(ClientId) == string.IsNullOrEmpty(ClientSecret);
}
