namespace TuTuMuch.Domain.Listings;

/// <summary>
/// One accepted bid on an auction (L2-115). It holds only the bidder's member id and the amount,
/// and is never shown to anyone but through the auction's current bid (L2-120).
/// </summary>
public sealed class Bid
{
    private Bid()
    {
    }

    public Guid Id { get; private set; }

    public Guid ListingId { get; private set; }

    public Guid BidderId { get; private set; }

    public decimal Amount { get; private set; }

    public DateTime PlacedAt { get; private set; }

    public static Bid Create(Guid listingId, Guid bidderId, decimal amount, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        ListingId = listingId,
        BidderId = bidderId,
        Amount = amount,
        PlacedAt = now,
    };
}

/// <summary>How a listing is sold (L2-113): at a fixed price, or by auction to the highest bidder.</summary>
public enum ListingFormat
{
    FixedPrice,
    Auction,
}

/// <summary>Why a bid was refused (L2-115).</summary>
public enum BidRefusal
{
    NotAnAuction,
    Ended,
    OwnListing,
    AlreadyHighest,
    Invalid,
    TooLow,
}

/// <summary>How an auction closed (L2-117).</summary>
public enum AuctionOutcome
{
    /// <summary>It had bids: Reserved for the winner until the payment window ends.</summary>
    Won,

    /// <summary>It had no bids: Removed, and the seller can relist it.</summary>
    Unsold,
}

/// <summary>
/// The result of <see cref="Listing.PlaceBid"/>: the stored bid, or why it was refused. Outbid is
/// the member who held the highest bid before this one, to be told by email (L2-119).
/// </summary>
public sealed record BidResult(Bid? Bid, BidRefusal? Refusal, Guid? Outbid)
{
    public static BidResult Refused(BidRefusal refusal) => new(null, refusal, null);
}
