namespace TuTuMuch.Domain.Members;

/// <summary>A single-use email verification link, stored as a hash, valid for 24 hours (L2-002).</summary>
public sealed class EmailVerificationToken
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private EmailVerificationToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid MemberId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? UsedAt { get; private set; }

    /// <summary>Set when a newer link is sent, so earlier links stop working.</summary>
    public DateTime? RevokedAt { get; private set; }

    public static EmailVerificationToken Issue(Guid memberId, string tokenHash, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        MemberId = memberId,
        TokenHash = tokenHash,
        CreatedAt = now,
        ExpiresAt = now + Lifetime,
    };

    public bool IsUsable(DateTime now) => UsedAt is null && RevokedAt is null && now <= ExpiresAt;

    public void MarkUsed(DateTime now) => UsedAt = now;

    public void Revoke(DateTime now) => RevokedAt ??= now;
}
