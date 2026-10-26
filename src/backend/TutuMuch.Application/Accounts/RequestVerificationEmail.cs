using MediatR;
using Microsoft.EntityFrameworkCore;
using TutuMuch.Application.Abstractions;
using TutuMuch.Application.Common;
using TutuMuch.Domain.Members;

namespace TutuMuch.Application.Accounts;

/// <summary>
/// Sends a new verification link. Answers the same for unknown and verified emails so it reveals
/// nothing; refuses a fourth link within an hour with 429 (L2-002 AC5, AC6).
/// </summary>
public sealed record RequestVerificationEmailCommand(string? Email) : IRequest<Result<Unit>>;

public sealed class RequestVerificationEmailCommandHandler(ITutuMuchDbContext db, VerificationEmails verificationEmails, TimeProvider clock)
    : IRequestHandler<RequestVerificationEmailCommand, Result<Unit>>
{
    public const int LinksPerHour = 3;

    public async Task<Result<Unit>> Handle(RequestVerificationEmailCommand command, CancellationToken cancellationToken)
    {
        var normalized = Member.Normalize(command.Email ?? string.Empty);
        var member = await db.Members.SingleOrDefaultAsync(m => m.NormalizedEmail == normalized, cancellationToken);
        if (member is null || member.IsEmailVerified)
        {
            return Unit.Value;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var since = now.AddHours(-1);
        var recent = await db.EmailVerificationTokens
            .Where(t => t.MemberId == member.Id && t.CreatedAt > since)
            .Select(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
        if (recent.Count >= LinksPerHour)
        {
            var retryAfter = recent.Min().AddHours(1) - now;
            return Error.TooManyRequests(retryAfter, "You can request 3 links an hour. Try again later.");
        }

        await verificationEmails.SendAsync(member, cancellationToken);
        return Unit.Value;
    }
}
