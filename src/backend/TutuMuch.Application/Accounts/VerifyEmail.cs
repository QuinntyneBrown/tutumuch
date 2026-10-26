using MediatR;
using Microsoft.EntityFrameworkCore;
using TutuMuch.Application.Abstractions;
using TutuMuch.Application.Common;

namespace TutuMuch.Application.Accounts;

/// <summary>Marks the member verified when the link is unused, unrevoked and under 24 hours old (L2-002).</summary>
public sealed record VerifyEmailCommand(string? Token) : IRequest<Result<Unit>>;

public sealed class VerifyEmailCommandHandler(ITutuMuchDbContext db, TimeProvider clock) : IRequestHandler<VerifyEmailCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return Used();
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var hash = SecureTokens.Hash(command.Token);
        var token = await db.EmailVerificationTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is null || token.UsedAt is not null || token.RevokedAt is not null)
        {
            return Used();
        }

        if (!token.IsUsable(now))
        {
            return new Error(400, "VERIFICATION_LINK_EXPIRED", "This link has expired");
        }

        var member = await db.Members.SingleAsync(m => m.Id == token.MemberId, cancellationToken);
        token.MarkUsed(now);
        member.MarkEmailVerified(now);
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private static Error Used() => new(400, "VERIFICATION_LINK_USED", "Link already used");
}
