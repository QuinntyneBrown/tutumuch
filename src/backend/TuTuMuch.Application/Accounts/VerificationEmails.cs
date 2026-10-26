using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Notifications;
using TuTuMuch.Domain.Members;
using TuTuMuch.Domain.Notifications;

namespace TuTuMuch.Application.Accounts;

/// <summary>
/// Issues a verification link, revoking every earlier link of the member (L2-002 AC5), and queues
/// the email with the new token in the same save (L2-042).
/// </summary>
public sealed class VerificationEmails(ITuTuMuchDbContext db, IEmailOutbox notifications, IOptions<AppOptions> app, TimeProvider clock)
{
    public async Task SendAsync(Member member, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var earlier = await db.EmailVerificationTokens
            .Where(t => t.MemberId == member.Id && t.UsedAt == null && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        earlier.ForEach(t => t.Revoke(now));

        var token = SecureTokens.Create();
        db.EmailVerificationTokens.Add(EmailVerificationToken.Issue(member.Id, SecureTokens.Hash(token), now));
        var link = $"{app.Value.BaseUrl.TrimEnd('/')}/verify?token={Uri.EscapeDataString(token)}";
        notifications.Enqueue(NotificationType.EmailVerification, member.Id, new EmailVerificationEmail(link));
        await db.SaveChangesAsync(cancellationToken);
    }
}
