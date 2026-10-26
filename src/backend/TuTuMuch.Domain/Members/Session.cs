namespace TuTuMuch.Domain.Members;

/// <summary>
/// One refresh token of a sign-in session, stored as a hash. Tokens rotate on every refresh;
/// all tokens issued from one sign-in share a family, and reuse revokes the family (L2-008).
/// </summary>
public sealed class Session
{
    public static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(14);

    private Session()
    {
    }

    public Guid Id { get; private set; }

    public Guid MemberId { get; private set; }

    public Guid FamilyId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    /// <summary>Set when this token was exchanged for a new one.</summary>
    public DateTime? RotatedAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public static Session Start(Guid memberId, string tokenHash, DateTime now) =>
        Create(memberId, Guid.NewGuid(), tokenHash, now);

    public bool IsActive(DateTime now) => RotatedAt is null && RevokedAt is null && now <= ExpiresAt;

    /// <summary>Marks this token used and returns its successor in the same family.</summary>
    public Session Rotate(string nextTokenHash, DateTime now)
    {
        RotatedAt = now;
        return Create(MemberId, FamilyId, nextTokenHash, now);
    }

    public void Revoke(DateTime now) => RevokedAt ??= now;

    private static Session Create(Guid memberId, Guid familyId, string tokenHash, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        MemberId = memberId,
        FamilyId = familyId,
        TokenHash = tokenHash,
        CreatedAt = now,
        ExpiresAt = now + RefreshLifetime,
    };
}
