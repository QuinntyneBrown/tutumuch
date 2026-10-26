using MediatR;
using Microsoft.EntityFrameworkCore;
using TutuMuch.Application.Abstractions;
using TutuMuch.Application.Common;
using TutuMuch.Domain.Members;

namespace TutuMuch.Application.Accounts;

/// <summary>A started or renewed session: the member and the raw refresh token for the cookie.</summary>
public sealed record SignedIn(Guid MemberId, string RefreshToken, DateTime RefreshExpiresAt);

/// <summary>Signs a member in with email and password; one answer for unknown email and wrong password (L2-006).</summary>
public sealed record SignInCommand(string? Email, string? Password) : IRequest<Result<SignedIn>>;

public sealed class SignInCommandHandler(ITutuMuchDbContext db, IPasswordHasher hasher, TimeProvider clock)
    : IRequestHandler<SignInCommand, Result<SignedIn>>
{
    public static readonly Error Incorrect = new(401, "SIGN_IN_FAILED", "Email or password is incorrect");

    public async Task<Result<SignedIn>> Handle(SignInCommand command, CancellationToken cancellationToken)
    {
        var normalized = Member.Normalize(command.Email ?? string.Empty);
        var member = await db.Members.SingleOrDefaultAsync(m => m.NormalizedEmail == normalized, cancellationToken);
        if (member is null || !hasher.Verify(member.PasswordHash, command.Password ?? string.Empty))
        {
            return Incorrect;
        }

        var token = SecureTokens.Create();
        var session = Session.Start(member.Id, SecureTokens.Hash(token), clock.GetUtcNow().UtcDateTime);
        db.Sessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return new SignedIn(member.Id, token, session.ExpiresAt);
    }
}

/// <summary>Exchanges a refresh token for a new one; a reused token revokes its whole family (L2-008).</summary>
public sealed record RefreshSessionCommand(string? RefreshToken) : IRequest<Result<SignedIn>>;

public sealed class RefreshSessionCommandHandler(ITutuMuchDbContext db, TimeProvider clock)
    : IRequestHandler<RefreshSessionCommand, Result<SignedIn>>
{
    public static readonly Error Refused = new(401, "SESSION_EXPIRED", "Sign in again");

    public async Task<Result<SignedIn>> Handle(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            return Refused;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var hash = SecureTokens.Hash(command.RefreshToken);
        var session = await db.Sessions.SingleOrDefaultAsync(s => s.TokenHash == hash, cancellationToken);
        if (session is null)
        {
            return Refused;
        }

        if (session.RotatedAt is not null)
        {
            var family = await db.Sessions.Where(s => s.FamilyId == session.FamilyId).ToListAsync(cancellationToken);
            family.ForEach(s => s.Revoke(now));
            await db.SaveChangesAsync(cancellationToken);
            return Refused;
        }

        if (!session.IsActive(now))
        {
            return Refused;
        }

        var token = SecureTokens.Create();
        var next = session.Rotate(SecureTokens.Hash(token), now);
        db.Sessions.Add(next);
        await db.SaveChangesAsync(cancellationToken);
        return new SignedIn(next.MemberId, token, next.ExpiresAt);
    }
}

/// <summary>Revokes the session family of the presented refresh token (L2-008 AC4).</summary>
public sealed record SignOutCommand(string? RefreshToken) : IRequest<Unit>;

public sealed class SignOutCommandHandler(ITutuMuchDbContext db, TimeProvider clock) : IRequestHandler<SignOutCommand, Unit>
{
    public async Task<Unit> Handle(SignOutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            return Unit.Value;
        }

        var hash = SecureTokens.Hash(command.RefreshToken);
        var session = await db.Sessions.SingleOrDefaultAsync(s => s.TokenHash == hash, cancellationToken);
        if (session is not null)
        {
            var now = clock.GetUtcNow().UtcDateTime;
            var family = await db.Sessions.Where(s => s.FamilyId == session.FamilyId).ToListAsync(cancellationToken);
            family.ForEach(s => s.Revoke(now));
            await db.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}

/// <summary>The signed-in member as the app shell needs it.</summary>
public sealed record CurrentMemberDto(string DisplayName, string Email, bool EmailVerified);

public sealed record GetCurrentMemberQuery(Guid MemberId) : IRequest<Result<CurrentMemberDto>>;

public sealed class GetCurrentMemberQueryHandler(ITutuMuchDbContext db) : IRequestHandler<GetCurrentMemberQuery, Result<CurrentMemberDto>>
{
    public async Task<Result<CurrentMemberDto>> Handle(GetCurrentMemberQuery query, CancellationToken cancellationToken)
    {
        var member = await db.Members.AsNoTracking()
            .Where(m => m.Id == query.MemberId)
            .Select(m => new CurrentMemberDto(m.DisplayName, m.Email, m.EmailVerifiedAt != null))
            .SingleOrDefaultAsync(cancellationToken);
        return member is null ? new Error(401, "SESSION_EXPIRED", "Sign in again") : member;
    }
}
