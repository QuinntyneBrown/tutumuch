namespace TuTuMuch.Domain.Members;

/// <summary>
/// A third-party identity whose email has no member yet, held between the provider's callback and
/// the finish-joining step: single use, valid for 10 minutes, identified by the hash of a token
/// that travels only in a cookie (L2-088).
/// </summary>
public sealed class PendingExternalJoin
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private PendingExternalJoin()
    {
    }

    public Guid Id { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    /// <summary>The email the provider verified; it becomes the member's verified email.</summary>
    public string Email { get; private set; } = string.Empty;

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? UsedAt { get; private set; }

    public static PendingExternalJoin Start(string provider, string subject, string email, string tokenHash, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        Provider = provider,
        Subject = subject,
        Email = email,
        TokenHash = tokenHash,
        CreatedAt = now,
        ExpiresAt = now + Lifetime,
    };

    public bool IsUsable(DateTime now) => UsedAt is null && now <= ExpiresAt;

    public void MarkUsed(DateTime now) => UsedAt ??= now;
}
