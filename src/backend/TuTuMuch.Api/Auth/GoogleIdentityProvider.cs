using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TuTuMuch.Application.Abstractions;

namespace TuTuMuch.Api.Auth;

/// <summary>
/// Google behind <see cref="IGoogleIdentityProvider"/> (L2-088, ADR accounts/0001): exchanges the
/// authorization code with the PKCE verifier at Google's token endpoint and validates the ID token's
/// signature (Google's published keys from its discovery document), issuer, audience and expiry.
/// It lives next to <see cref="SessionCookies"/> because it reuses the API's own token libraries.
/// Neither the code nor a token is ever logged.
/// </summary>
public sealed class GoogleIdentityProvider(
    IOptions<GoogleOptions> options,
    IHttpClientFactory httpClientFactory,
    TimeProvider clock,
    ILogger<GoogleIdentityProvider> logger) : IGoogleIdentityProvider
{
    public const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    public const string DiscoveryDocument = "https://accounts.google.com/.well-known/openid-configuration";
    private static readonly string[] Issuers = ["https://accounts.google.com", "accounts.google.com"];

    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configuration =
        new(DiscoveryDocument, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever { RequireHttps = true });

    private readonly JsonWebTokenHandler _tokens = new() { MapInboundClaims = false };

    public string? ClientId => options.Value.IsConfigured ? options.Value.ClientId : null;

    public async Task<GoogleIdentity?> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken)
    {
        if (ClientId is null)
        {
            return null;
        }

        using var client = httpClientFactory.CreateClient(nameof(GoogleIdentityProvider));
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["code_verifier"] = codeVerifier,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = ClientId,
            ["client_secret"] = options.Value.ClientSecret,
        });
        using var response = await client.PostAsync(TokenEndpoint, form, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Google refused the authorization code with HTTP {StatusCode}", (int)response.StatusCode);
            return null;
        }

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (string.IsNullOrEmpty(tokens?.IdToken))
        {
            logger.LogWarning("Google's token response carried no ID token");
            return null;
        }

        var configuration = await _configuration.GetConfigurationAsync(cancellationToken);
        var result = await _tokens.ValidateTokenAsync(tokens.IdToken, new TokenValidationParameters
        {
            ValidIssuers = Issuers,
            ValidAudience = ClientId,
            IssuerSigningKeys = configuration.SigningKeys,
            ClockSkew = TimeSpan.Zero,
            LifetimeValidator = (notBefore, expires, _, _) =>
            {
                var now = clock.GetUtcNow().UtcDateTime;
                return (notBefore is null || notBefore <= now) && expires > now;
            },
        });
        if (!result.IsValid || result.ClaimsIdentity is null)
        {
            logger.LogWarning(result.Exception, "Google's ID token was rejected");
            return null;
        }

        var claims = result.ClaimsIdentity;
        var verified = bool.TryParse(claims.FindFirst("email_verified")?.Value, out var emailVerified) && emailVerified;
        return new GoogleIdentity(
            claims.FindFirst("sub")?.Value ?? string.Empty,
            claims.FindFirst("email")?.Value ?? string.Empty,
            verified,
            claims.FindFirst("nonce")?.Value);
    }

    private sealed record TokenResponse([property: JsonPropertyName("id_token")] string? IdToken);
}
