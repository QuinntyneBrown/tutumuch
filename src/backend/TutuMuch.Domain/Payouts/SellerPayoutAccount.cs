namespace TutuMuch.Domain.Payouts;

/// <summary>Onboarding state of a seller's connected account. NotStarted has no row.</summary>
public enum PayoutAccountStatus
{
    NotStarted,
    Pending,
    Complete,
    Restricted,
}

/// <summary>
/// A seller's connected account at the payment provider (L2-031). Tutu Much keeps the bank's name and
/// last four digits for display only, never a full account number. Only the provider's signed
/// account.updated webhook marks onboarding Complete, never the browser's return.
/// </summary>
public sealed class SellerPayoutAccount
{
    private SellerPayoutAccount()
    {
    }

    public Guid MemberId { get; private set; }

    public string ProviderAccountId { get; private set; } = string.Empty;

    public PayoutAccountStatus Status { get; private set; }

    public string? BankName { get; private set; }

    public string? BankLast4 { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>"Chase •••• 6021", or null before the provider reported a bank.</summary>
    public string? BankDisplay => BankName is null || BankLast4 is null ? null : $"{BankName} •••• {BankLast4}";

    public static SellerPayoutAccount Start(Guid memberId, string providerAccountId, DateTime now) => new()
    {
        MemberId = memberId,
        ProviderAccountId = providerAccountId,
        Status = PayoutAccountStatus.Pending,
        UpdatedAt = now,
    };

    /// <summary>The provider's report; returns whether onboarding just became Complete.</summary>
    public bool Update(bool complete, string? bankName, string? bankLast4, DateTime now)
    {
        var wasComplete = Status == PayoutAccountStatus.Complete;
        Status = complete ? PayoutAccountStatus.Complete : wasComplete ? PayoutAccountStatus.Restricted : PayoutAccountStatus.Pending;
        BankName = bankName ?? BankName;
        BankLast4 = bankLast4 ?? BankLast4;
        UpdatedAt = now;
        return complete && !wasComplete;
    }
}
