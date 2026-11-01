namespace TuTuMuch.Application.Grabs;

/// <summary>The deposit rule of ADR orders/0009, bound from the "Grabs" section.</summary>
public sealed class GrabOptions
{
    public const string Section = "Grabs";

    /// <summary>The deposit as a percentage of the shelf price (20 for 20%).</summary>
    public decimal DepositPercent { get; set; } = 20m;

    /// <summary>The smallest deposit, unless the shelf price is lower.</summary>
    public decimal MinimumDeposit { get; set; } = 5m;
}

/// <summary>The amounts a shopper sees before paying (L2-117). Amounts are "0.00" strings.</summary>
public sealed record GrabQuoteDto(
    string FindReference,
    string FindTitle,
    string StoreName,
    string ShelfPrice,
    string GrabFee,
    string SalesTax,
    string Total,
    string Deposit,
    string RefundAfterAcceptance);

public sealed record GrabAddressDto(string FullName, string Line1, string? Line2, string City, string State, string Zip);

/// <summary>The shopper's grab (L2-119). RefundIfCancelled is null when it can no longer be cancelled.</summary>
public sealed record GrabDto(
    Guid Id,
    string Reference,
    string Status,
    string FindReference,
    string FindTitle,
    string Category,
    string StoreName,
    string StoreAddress,
    string ShelfPrice,
    string GrabFee,
    string SalesTax,
    string Total,
    string Deposit,
    string? RefundIfCancelled,
    string? RefundedAmount,
    string? UnavailableReason,
    string? PaymentDeclineReason,
    DateTime? AnswerBy,
    DateTime? BuyBy,
    DateTime? BoughtAt,
    DateTime CreatedAt,
    GrabAddressDto Address);

/// <summary>One row of the shopper's grabs (L2-119).</summary>
public sealed record GrabSummaryDto(
    Guid Id,
    string Reference,
    string Status,
    string FindReference,
    string FindTitle,
    string StoreName,
    string Total,
    DateTime? Deadline,
    DateTime CreatedAt);

/// <summary>One grab on the administrator's list (L2-118). The address is shown only while TuTuMuch needs it.</summary>
public sealed record AdminGrabDto(
    Guid Id,
    string Reference,
    string Status,
    string FindReference,
    string FindTitle,
    string StoreName,
    string ShopperDisplayName,
    string ShopperEmail,
    string Total,
    string Deposit,
    string? RefundedAmount,
    string? UnavailableReason,
    DateTime? Deadline,
    GrabAddressDto? Address,
    DateTime CreatedAt);
