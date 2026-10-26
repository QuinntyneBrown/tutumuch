using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Domain.Notifications;

namespace TuTuMuch.Application.Notifications;

/// <summary>
/// One pass of the email dispatcher (L2-042): claims up to <see cref="BatchSize"/> due outbox
/// messages and, for each, resolves the recipient, renders the template, runs the content guard and
/// sends through the provider with the message ID as idempotency key. An email of a category the
/// member turned off is suppressed (L2-043), and a marketing email gets the one-click unsubscribe
/// link and the RFC 8058 List-Unsubscribe headers. A transient failure is
/// retried with back-off for 24 hours; a rejected address or a guard hit fails at once. Each
/// failure is logged with the message ID, type and attempt, never the address or the content.
/// Returns how many emails were sent.
/// </summary>
public sealed record DispatchEmailsCommand : IRequest<int>
{
    public const int BatchSize = 50;
}

public sealed partial class DispatchEmailsHandler(
    ITuTuMuchDbContext db,
    IEmailSender sender,
    INotificationPreferenceReader preferences,
    UnsubscribeTokens unsubscribeTokens,
    IOptions<AppOptions> app,
    TimeProvider clock,
    ILogger<DispatchEmailsHandler> logger) : IRequestHandler<DispatchEmailsCommand, int>
{
    public async Task<int> Handle(DispatchEmailsCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var due = await db.OutboxMessages
            .Where(m => (m.Status == OutboxMessageStatus.Pending && m.NextAttemptAt <= now) ||
                        (m.Status == OutboxMessageStatus.Processing && m.LockedUntil <= now))
            .OrderBy(m => m.NextAttemptAt)
            .Select(m => m.Id)
            .Take(DispatchEmailsCommand.BatchSize)
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var id in due)
        {
            if (await ClaimAsync(id, now, cancellationToken) is { } message && await DispatchAsync(message, cancellationToken))
            {
                sent++;
            }
        }

        return sent;
    }

    /// <summary>Claims the message; null when another worker claimed it first (the claim token changed).</summary>
    private async Task<OutboxMessage?> ClaimAsync(Guid id, DateTime now, CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        var message = await db.OutboxMessages.SingleAsync(m => m.Id == id, cancellationToken);
        if (!message.IsClaimable(now))
        {
            return null;
        }

        try
        {
            message.Claim(now);
            await db.SaveChangesAsync(cancellationToken);
            return message;
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }
    }

    private async Task<bool> DispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var recipient = await db.Members.AsNoTracking()
            .Where(m => m.Id == message.RecipientMemberId)
            .Select(m => new { m.Email, m.DisplayName })
            .SingleOrDefaultAsync(cancellationToken);
        if (recipient is null)
        {
            message.Suppress("recipient_unavailable");
            await db.SaveChangesAsync(cancellationToken);
            return false;
        }

        var category = NotificationCategories.Of(message.NotificationType);
        if (!await preferences.IsAllowedAsync(message.RecipientMemberId, category, cancellationToken))
        {
            message.Suppress("preference_off");
            await db.SaveChangesAsync(cancellationToken);
            return false;
        }

        var email = EmailTemplates.Render(message.NotificationType, message.Payload, recipient.DisplayName, app.Value.BaseUrl);
        IReadOnlyDictionary<string, string>? headers = null;
        if (category == NotificationCategory.Marketing)
        {
            (email, headers) = WithUnsubscribe(email, message.RecipientMemberId);
        }

        if (EmailContentGuard.Check(email.Subject + "\n" + email.Body, email.Identifiers) is { } reason)
        {
            message.Fail($"content_guard:{reason}");
            await db.SaveChangesAsync(cancellationToken);
            LogGuardHit(logger, message.Id, message.NotificationType, reason);
            return false;
        }

        try
        {
            await sender.SendAsync(new EmailMessage(recipient.Email, email.Subject, email.Body, message.Id, headers), cancellationToken);
            message.RecordSent(clock.GetUtcNow().UtcDateTime);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (EmailRejectedException exception)
        {
            message.Fail($"rejected:{exception.Message}");
            await db.SaveChangesAsync(cancellationToken);
            LogFinalFailure(logger, message.Id, message.NotificationType, message.AttemptCount, message.LastError!);
            return false;
        }
        catch (Exception exception) when (exception is HttpRequestException or TimeoutException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            var error = exception is HttpRequestException { StatusCode: { } status } ? $"http_{(int)status}" : exception.GetType().Name;
            var final = message.RecordFailure(error, clock.GetUtcNow().UtcDateTime, Random.Shared.NextDouble());
            await db.SaveChangesAsync(cancellationToken);
            if (final)
            {
                LogFinalFailure(logger, message.Id, message.NotificationType, message.AttemptCount, error);
            }
            else
            {
                LogRetry(logger, message.Id, message.NotificationType, message.AttemptCount, error, message.NextAttemptAt);
            }

            return false;
        }
    }

    /// <summary>
    /// Adds the unsubscribe link to TuTuMuch Web's unsubscribe page and the List-Unsubscribe headers that
    /// let a mail client POST the token to TuTuMuch API, which TuTuMuch Web serves under the same origin.
    /// </summary>
    private (RenderedEmail Email, IReadOnlyDictionary<string, string> Headers) WithUnsubscribe(RenderedEmail email, Guid memberId)
    {
        var root = app.Value.BaseUrl.TrimEnd('/');
        var token = Uri.EscapeDataString(unsubscribeTokens.Issue(memberId, NotificationCategory.Marketing, clock.GetUtcNow().UtcDateTime));
        var page = $"{root}/emails/unsubscribe?token={token}";
        var headers = new Dictionary<string, string>
        {
            ["List-Unsubscribe"] = $"<{root}/api/notifications/unsubscribe?token={token}>",
            ["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click",
        };
        return (email with { Body = $"{email.Body}\nUnsubscribe from news from TuTuMuch in one click: {page}", Identifiers = [.. email.Identifiers, page] }, headers);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Email {OutboxMessageId} ({NotificationType}) attempt {AttemptCount} failed with {Error}; retrying at {NextAttemptAt:o}")]
    private static partial void LogRetry(ILogger logger, Guid outboxMessageId, NotificationType notificationType, int attemptCount, string error, DateTime nextAttemptAt);

    [LoggerMessage(Level = LogLevel.Error, Message = "Email {OutboxMessageId} ({NotificationType}) failed for good after {AttemptCount} attempts: {Error}")]
    private static partial void LogFinalFailure(ILogger logger, Guid outboxMessageId, NotificationType notificationType, int attemptCount, string error);

    [LoggerMessage(Level = LogLevel.Error, Message = "Email {OutboxMessageId} ({NotificationType}) was not sent: the content guard found a {Reason}")]
    private static partial void LogGuardHit(ILogger logger, Guid outboxMessageId, NotificationType notificationType, string reason);
}
