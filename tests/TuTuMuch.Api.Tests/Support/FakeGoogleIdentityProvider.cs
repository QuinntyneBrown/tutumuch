using TuTuMuch.Application.Abstractions;

namespace TuTuMuch.Api.Tests.Support;

/// <summary>
/// Google behind the port, without Google: a test registers the identity a code stands for (with
/// the nonce the start endpoint issued), and each code is accepted once, as Google accepts it.
/// ClientId null switches the feature off, as an unset Auth:Google does.
/// </summary>
public sealed class FakeGoogleIdentityProvider : IGoogleIdentityProvider
{
    public string? ClientId { get; set; } = "test-google-client";

    public Dictionary<string, GoogleIdentity> Identities { get; } = new(StringComparer.Ordinal);

    public List<(string Code, string CodeVerifier, string RedirectUri)> Exchanges { get; } = [];

    public Task<GoogleIdentity?> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken)
    {
        Exchanges.Add((code, codeVerifier, redirectUri));
        return Task.FromResult<GoogleIdentity?>(Identities.Remove(code, out var identity) ? identity : null);
    }
}
