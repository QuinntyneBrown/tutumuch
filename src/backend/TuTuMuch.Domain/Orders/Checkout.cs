namespace TuTuMuch.Domain.Orders;

/// <summary>Where one checkout stands. Open and Processing hold the listing (L2-028).</summary>
public enum CheckoutStatus
{
    Open,
    Processing,
    Paid,
    Released,
    Expired,
}

/// <summary>
/// One buyer's attempt to buy one listing, from "Buy now" to payment or release. It holds the
/// listing for <see cref="ReservationDuration"/> and snapshots the prices when it starts.
/// </summary>
public sealed class Checkout
{
    public static readonly TimeSpan ReservationDuration = TimeSpan.FromMinutes(10);

    private Checkout()
    {
    }

    public Guid Id { get; private set; }

    public Guid ListingId { get; private set; }

    public Guid BuyerId { get; private set; }

    public CheckoutStatus Status { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public decimal ItemPrice { get; private set; }

    public decimal ShippingCost { get; private set; }

    public decimal? SalesTax { get; private set; }

    public decimal? Total { get; private set; }

    public ShippingAddress? Address { get; private set; }

    public string? LastDeclineReason { get; private set; }

    public Guid? OrderId { get; private set; }

    public DateTime? EndedAt { get; private set; }

    /// <summary>True while the checkout holds its listing.</summary>
    public bool HoldsListing => Status is CheckoutStatus.Open or CheckoutStatus.Processing;

    public static Checkout Start(Guid id, Guid listingId, Guid buyerId, decimal itemPrice, decimal shippingCost, DateTime now) => new()
    {
        Id = id,
        ListingId = listingId,
        BuyerId = buyerId,
        Status = CheckoutStatus.Open,
        StartedAt = now,
        ExpiresAt = now + ReservationDuration,
        ItemPrice = itemPrice,
        ShippingCost = shippingCost,
    };

    /// <summary>True while the checkout holds its listing and its 10 minutes have not run out.</summary>
    public bool IsLive(DateTime now) => HoldsListing && now < ExpiresAt;

    /// <summary>Sets the address and the provider's tax quote; the total is item plus shipping plus tax.</summary>
    public void SetAddress(ShippingAddress address, decimal salesTax)
    {
        EnsureOpen();
        Address = address;
        SalesTax = salesTax;
        Total = ItemPrice + ShippingCost + salesTax;
    }

    /// <summary>A payment was sent to the provider; the result comes from its webhook (L2-030).</summary>
    public void BeginPayment()
    {
        EnsureOpen();
        Status = CheckoutStatus.Processing;
        LastDeclineReason = null;
    }

    /// <summary>The provider declined the card: the buyer can try again while the reservation lasts (L2-030 AC3).</summary>
    public void RecordDecline(string reason)
    {
        if (Status == CheckoutStatus.Processing)
        {
            Status = CheckoutStatus.Open;
        }

        LastDeclineReason = reason;
    }

    public void MarkPaid(Guid orderId, DateTime now)
    {
        Status = CheckoutStatus.Paid;
        OrderId = orderId;
        EndedAt = now;
    }

    /// <summary>Ends the checkout without an order: the buyer cancelled or the 10 minutes ran out.</summary>
    public void Release(CheckoutStatus reason, DateTime now)
    {
        if (reason is not (CheckoutStatus.Released or CheckoutStatus.Expired))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        if (HoldsListing)
        {
            Status = reason;
            EndedAt = now;
        }
    }

    private void EnsureOpen()
    {
        if (Status != CheckoutStatus.Open)
        {
            throw new InvalidOperationException("Only an open checkout can change.");
        }
    }
}
