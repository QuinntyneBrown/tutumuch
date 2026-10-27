namespace TuTuMuch.Domain.Members;

/// <summary>
/// A third-party account linked to a member, keyed by the provider's stable subject and never by
/// email (L2-088). Deleted with the member.
/// </summary>
public sealed class ExternalLogin
{
    public const string Google = "google";

    private ExternalLogin()
    {
    }

    public Guid Id { get; private set; }

    public Guid MemberId { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    /// <summary>The provider's identifier for the account, such as Google's <c>sub</c>.</summary>
    public string Subject { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public static ExternalLogin Link(Guid memberId, string provider, string subject, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        MemberId = memberId,
        Provider = provider,
        Subject = subject,
        CreatedAt = now,
    };
}
