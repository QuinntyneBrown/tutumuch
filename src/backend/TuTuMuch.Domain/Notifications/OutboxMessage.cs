namespace TuTuMuch.Domain.Notifications;

/// <summary>Where an outbox message is. Sent, Suppressed and Failed are terminal.</summary>
public enum OutboxMessageStatus
{
    Pending,
    Processing,
    Sent,
    Suppressed,
    Failed,
}

/// <summary>
/// One email request, written in the same transaction as the change that caused it, so an email
/// is never lost or sent for a change that rolled back (L2-042). It names the recipient member,
/// not an address, and the template model as JSON. The dispatcher claims it, sends it with its ID
/// as idempotency key, and retries failures with back-off of 1, 2, 4 ... up to 60 minutes, plus up
/// to 20% jitter, until 24 hours after it was created.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>How long after creation a failing message is retried (L2-042 AC2).</summary>
    public static readonly TimeSpan RetryWindow = TimeSpan.FromHours(24);

    /// <summary>The longest wait between two attempts.</summary>
    public static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(60);

    /// <summary>How long a claim lasts; a message whose claim ran out (a crashed worker) is claimable again.</summary>
    public static readonly TimeSpan ClaimFor = TimeSpan.FromMinutes(2);

    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }

    public NotificationType NotificationType { get; private set; }

    public Guid RecipientMemberId { get; private set; }

    /// <summary>The template model as JSON. Never a password, card number or security code.</summary>
    public string Payload { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public OutboxMessageStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTime NextAttemptAt { get; private set; }

    public DateTime? LockedUntil { get; private set; }

    /// <summary>The provider status or reason of the last failure; never the address or the content.</summary>
    public string? LastError { get; private set; }

    public DateTime? SentAt { get; private set; }

    public string? SuppressionReason { get; private set; }

    /// <summary>Concurrency token renewed by every change, so two workers cannot claim the same message.</summary>
    public Guid ClaimToken { get; private set; }

    /// <summary>A Pending message, due at once.</summary>
    public static OutboxMessage Create(NotificationType type, Guid recipientMemberId, string payload, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        NotificationType = type,
        RecipientMemberId = recipientMemberId,
        Payload = payload,
        CreatedAt = now,
        Status = OutboxMessageStatus.Pending,
        NextAttemptAt = now,
        ClaimToken = Guid.NewGuid(),
    };

    /// <summary>Whether a worker may claim it at <paramref name="now"/>: Pending and due, or Processing with a lapsed claim.</summary>
    public bool IsClaimable(DateTime now) =>
        (Status == OutboxMessageStatus.Pending && NextAttemptAt <= now) ||
        (Status == OutboxMessageStatus.Processing && LockedUntil <= now);

    public void Claim(DateTime now)
    {
        if (!IsClaimable(now))
        {
            throw new InvalidOperationException("The outbox message is not claimable.");
        }

        Status = OutboxMessageStatus.Processing;
        LockedUntil = now + ClaimFor;
        ClaimToken = Guid.NewGuid();
    }

    public void RecordSent(DateTime now)
    {
        EnsureProcessing();
        AttemptCount++;
        Status = OutboxMessageStatus.Sent;
        SentAt = now;
        LockedUntil = null;
        ClaimToken = Guid.NewGuid();
    }

    /// <summary>Not sent on purpose, for example because the recipient no longer exists.</summary>
    public void Suppress(string reason)
    {
        EnsureProcessing();
        Status = OutboxMessageStatus.Suppressed;
        SuppressionReason = reason;
        LockedUntil = null;
        ClaimToken = Guid.NewGuid();
    }

    /// <summary>
    /// A transient failure: retried after <c>min(2^(attempts − 1) minutes, 60 minutes)</c> plus
    /// <paramref name="jitter"/> (0 to 1) of up to 20% more; Failed once the retry window has passed.
    /// Returns whether the failure is final.
    /// </summary>
    public bool RecordFailure(string error, DateTime now, double jitter)
    {
        EnsureProcessing();
        AttemptCount++;
        LastError = Truncate(error);
        LockedUntil = null;
        ClaimToken = Guid.NewGuid();
        var minutes = Math.Min(Math.Pow(2, AttemptCount - 1), MaxBackoff.TotalMinutes);
        var next = now + TimeSpan.FromMinutes(minutes * (1 + (0.2 * Math.Clamp(jitter, 0, 1))));
        if (next > CreatedAt + RetryWindow)
        {
            Status = OutboxMessageStatus.Failed;
            return true;
        }

        Status = OutboxMessageStatus.Pending;
        NextAttemptAt = next;
        return false;
    }

    /// <summary>A permanent failure, such as a rejected address or content the guard refused: never retried.</summary>
    public void Fail(string error)
    {
        EnsureProcessing();
        AttemptCount++;
        LastError = Truncate(error);
        Status = OutboxMessageStatus.Failed;
        LockedUntil = null;
        ClaimToken = Guid.NewGuid();
    }

    private void EnsureProcessing()
    {
        if (Status != OutboxMessageStatus.Processing)
        {
            throw new InvalidOperationException("The outbox message is not claimed.");
        }
    }

    private static string Truncate(string error) => error.Length <= 1000 ? error : error[..1000];
}
