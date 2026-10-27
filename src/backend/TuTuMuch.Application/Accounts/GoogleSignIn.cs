using MediatR;
using Microsoft.EntityFrameworkCore;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Common;
using TuTuMuch.Domain.Members;

namespace TuTuMuch.Application.Accounts;

/// <summary>Builds the Google consent URL for one flow (L2-088, ADR accounts/0001).</summary>
public static class GoogleAuthorization
{
    public const string Endpoint = "https://accounts.google.com/o/oauth2/v2/auth";

    public static string Url(string clientId, string redirectUri, string state, string nonce, string codeChallenge)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid email",
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["prompt"] = "select_account",
        };
        return Endpoint + "?" + string.Join("&", query.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
    }
}

/// <summary>
/// What a Google callback leads to: a session for a member who exists or was linked by their
/// verified email, or the raw token of a pending join for a new person.
/// </summary>
public sealed record GoogleSignInOutcome(SignedIn? SignedIn, string? JoinToken);

/// <summary>
/// Exchanges the callback's code and signs in, links or starts a join (L2-088). The nonce is the
/// one the start endpoint sent; the redirect URI is the one Google called back.
/// </summary>
public sealed record GoogleCallbackCommand(string? Code, string CodeVerifier, string RedirectUri, string ExpectedNonce)
    : IRequest<Result<GoogleSignInOutcome>>;

public sealed class GoogleCallbackCommandHandler(ITuTuMuchDbContext db, IGoogleIdentityProvider google, TimeProvider clock)
    : IRequestHandler<GoogleCallbackCommand, Result<GoogleSignInOutcome>>
{
    public static readonly Error Failed = new(401, "GOOGLE_SIGN_IN_FAILED", "Couldn't sign in with Google");
    public static readonly Error EmailUnverified = new(403, "GOOGLE_EMAIL_UNVERIFIED", "Verify the email with Google first");
    public static readonly Error AccountExists = new(409, "GOOGLE_ACCOUNT_EXISTS", "An account with this email already exists");

    public async Task<Result<GoogleSignInOutcome>> Handle(GoogleCallbackCommand command, CancellationToken cancellationToken)
    {
        if (google.ClientId is null)
        {
            return Error.NotFound("GOOGLE_NOT_CONFIGURED");
        }

        if (string.IsNullOrEmpty(command.Code))
        {
            return Failed;
        }

        var identity = await google.ExchangeCodeAsync(command.Code, command.CodeVerifier, command.RedirectUri, cancellationToken);
        if (identity is null || identity.Nonce != command.ExpectedNonce || string.IsNullOrEmpty(identity.Subject) || string.IsNullOrEmpty(identity.Email))
        {
            return Failed;
        }

        if (!identity.EmailVerified)
        {
            return EmailUnverified;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var login = await db.ExternalLogins.SingleOrDefaultAsync(l => l.Provider == ExternalLogin.Google && l.Subject == identity.Subject, cancellationToken);
        if (login is not null)
        {
            var linked = await db.Members.SingleAsync(m => m.Id == login.MemberId, cancellationToken);
            if (linked.IsClosed)
            {
                return Failed;
            }

            return new GoogleSignInOutcome(await SessionIssuer.StartAsync(db, linked.Id, now, cancellationToken), null);
        }

        var normalized = Member.Normalize(identity.Email);
        var member = await db.Members.SingleOrDefaultAsync(m => m.NormalizedEmail == normalized, cancellationToken);
        if (member is not null)
        {
            if (member.IsClosed)
            {
                return Failed;
            }

            if (!member.IsEmailVerified)
            {
                return AccountExists;
            }

            db.ExternalLogins.Add(ExternalLogin.Link(member.Id, ExternalLogin.Google, identity.Subject, now));
            return new GoogleSignInOutcome(await SessionIssuer.StartAsync(db, member.Id, now, cancellationToken), null);
        }

        var token = SecureTokens.Create();
        db.PendingExternalJoins.Add(PendingExternalJoin.Start(ExternalLogin.Google, identity.Subject, identity.Email.Trim(), SecureTokens.Hash(token), now));
        await db.SaveChangesAsync(cancellationToken);
        return new GoogleSignInOutcome(null, token);
    }
}

/// <summary>The email of a pending Google join, for the finish-joining page (L2-088).</summary>
public sealed record PendingGoogleJoinDto(string Email);

public sealed record GetPendingGoogleJoinQuery(string? Token) : IRequest<Result<PendingGoogleJoinDto>>;

public sealed class GetPendingGoogleJoinQueryHandler(ITuTuMuchDbContext db, IGoogleIdentityProvider google, TimeProvider clock)
    : IRequestHandler<GetPendingGoogleJoinQuery, Result<PendingGoogleJoinDto>>
{
    public static readonly Error Expired = new(410, "GOOGLE_JOIN_EXPIRED", "Start again");

    public async Task<Result<PendingGoogleJoinDto>> Handle(GetPendingGoogleJoinQuery query, CancellationToken cancellationToken)
    {
        if (google.ClientId is null)
        {
            return Error.NotFound("GOOGLE_NOT_CONFIGURED");
        }

        var pending = await PendingJoins.FindUsableAsync(db, query.Token, clock.GetUtcNow().UtcDateTime, cancellationToken);
        return pending is null ? Expired : new PendingGoogleJoinDto(pending.Email);
    }
}

/// <summary>
/// Finishes a Google join: the display name, age and terms rules of L2-001, then a verified member
/// with no password, the Google link and a session (L2-088).
/// </summary>
public sealed record FinishGoogleJoinCommand(string? Token, string? DisplayName, bool AgeConfirmed, bool TermsAccepted) : IRequest<Result<SignedIn>>;

public sealed class FinishGoogleJoinCommandHandler(ITuTuMuchDbContext db, IGoogleIdentityProvider google, TimeProvider clock)
    : IRequestHandler<FinishGoogleJoinCommand, Result<SignedIn>>
{
    public async Task<Result<SignedIn>> Handle(FinishGoogleJoinCommand command, CancellationToken cancellationToken)
    {
        if (google.ClientId is null)
        {
            return Error.NotFound("GOOGLE_NOT_CONFIGURED");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var pending = await PendingJoins.FindUsableAsync(db, command.Token, now, cancellationToken);
        if (pending is null)
        {
            return GetPendingGoogleJoinQueryHandler.Expired;
        }

        var displayName = command.DisplayName?.Trim() ?? string.Empty;
        var errors = new Dictionary<string, FieldError>();
        await MemberRegistrationRules.ValidateDisplayNameAsync(db, displayName, errors, cancellationToken);
        MemberRegistrationRules.ValidateConsent(command.AgeConfirmed, command.TermsAccepted, errors);
        if (await db.Members.AnyAsync(m => m.NormalizedEmail == Member.Normalize(pending.Email), cancellationToken))
        {
            errors["email"] = new("EMAIL_TAKEN", "An account with this email already exists");
        }

        if (errors.Count > 0)
        {
            return Error.Invalid("GOOGLE_JOIN_INVALID", errors);
        }

        var member = Member.JoinVerified(displayName, pending.Email, now);
        db.Members.Add(member);
        db.ExternalLogins.Add(ExternalLogin.Link(member.Id, pending.Provider, pending.Subject, now));
        pending.MarkUsed(now);
        await db.SaveChangesAsync(cancellationToken);
        return await SessionIssuer.StartAsync(db, member.Id, now, cancellationToken);
    }
}

internal static class PendingJoins
{
    /// <summary>The pending join behind a raw token when it exists, is unused and is under 10 minutes old.</summary>
    public static async Task<PendingExternalJoin?> FindUsableAsync(ITuTuMuchDbContext db, string? token, DateTime now, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var hash = SecureTokens.Hash(token);
        var pending = await db.PendingExternalJoins.SingleOrDefaultAsync(j => j.TokenHash == hash, cancellationToken);
        return pending is not null && pending.IsUsable(now) ? pending : null;
    }
}
