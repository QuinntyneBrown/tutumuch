// Unit Test
// Traces to: L2-113, L2-115, L2-116, L2-117, L2-118, L2-120
// Description: An auction is a listing format: it publishes with an end time, takes bids under the
// minimum and ceiling rules, closes to its winner or as unsold, keeps its hold for the winner until
// the payment window ends, relists as a new run, and cannot change once it has bids.

using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Domain.Tests;

public sealed class AuctionTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Seller = Guid.NewGuid();
    private static readonly Guid Ann = Guid.NewGuid();
    private static readonly Guid Bea = Guid.NewGuid();

    private static ListingDetails Details(string? format = "Auction", int? days = 5, decimal? price = 12.00m) => new(
        "Clothing", "Dresses", null, "Rainbow tulle tutu, pink", "Light wear, colours still bright.", "Gymboree", "Used - Good", null, price, 5.00m, format, days);

    private static Listing Running(int days = 5)
    {
        var listing = Listing.CreateDraft(Seller, 10001, Details(days: days), Now);
        Assert.Empty(listing.Publish(Now, photoCount: 1));
        return listing;
    }

    [Fact]
    public void Publishing_an_auction_sets_its_end_time_from_its_length()
    {
        var listing = Running(days: 5);

        Assert.True(listing.IsAuction);
        Assert.Equal(ListingStatus.Active, listing.Status);
        Assert.Equal(new DateTime(2026, 11, 6, 10, 0, 0, DateTimeKind.Utc), listing.AuctionEndsAt);
        Assert.Equal(12.00m, listing.CurrentPrice);
        Assert.Equal(12.00m, listing.MinimumBid);
    }

    [Fact]
    public void A_fixed_price_listing_is_the_default_and_has_no_auction_fields()
    {
        var listing = Listing.CreateDraft(Seller, 10001, Details(format: null, days: 5), Now);

        Assert.Equal(ListingFormat.FixedPrice, listing.Format);
        Assert.Null(listing.AuctionDays);
        Assert.Empty(listing.Publish(Now, photoCount: 1));
        Assert.Null(listing.AuctionEndsAt);
    }

    [Fact]
    public void An_auction_needs_a_length_of_3_5_or_7_days()
    {
        Assert.Equal("AUCTION_LENGTH_INVALID", Assert.Single(Listing.CheckDraft(Details(days: 4))).Code);
        Assert.Equal("FORMAT_INVALID", Assert.Single(Listing.CheckDraft(Details(format: "Raffle"))).Code);
        Assert.Empty(Listing.CheckDraft(Details(days: null)));

        var draft = Listing.CreateDraft(Seller, 10001, Details(days: null), Now);
        var violation = Assert.Single(draft.Publish(Now, photoCount: 1));
        Assert.Equal(("auctionDays", "AUCTION_LENGTH_REQUIRED"), (violation.Field, violation.Code));
        Assert.Equal(ListingStatus.Draft, draft.Status);
    }

    [Fact]
    public void A_bid_must_reach_the_minimum_and_stay_in_whole_cents_under_the_ceiling()
    {
        var listing = Running();

        Assert.Equal(BidRefusal.TooLow, listing.PlaceBid(Ann, 11.99m, Now).Refusal);
        Assert.Equal(BidRefusal.Invalid, listing.PlaceBid(Ann, 12.345m, Now).Refusal);
        Assert.Equal(BidRefusal.Invalid, listing.PlaceBid(Ann, 5000.01m, Now).Refusal);
        Assert.Equal(0, listing.BidCount);

        var first = listing.PlaceBid(Ann, 12.00m, Now);
        Assert.Null(first.Refusal);
        Assert.Equal(12.00m, first.Bid!.Amount);
        Assert.Null(first.Outbid);
        Assert.Equal(13.00m, listing.MinimumBid);

        Assert.Equal(BidRefusal.TooLow, listing.PlaceBid(Bea, 12.50m, Now).Refusal);
        var second = listing.PlaceBid(Bea, 13.00m, Now);
        Assert.Equal(Ann, second.Outbid);
        Assert.Equal(13.00m, listing.CurrentBid);
        Assert.Equal(2, listing.BidCount);
        Assert.Equal(Bea, listing.HighBidderId);
    }

    [Fact]
    public void Every_bid_bumps_the_version_so_a_racing_bid_or_edit_loses()
    {
        var listing = Running();
        var before = listing.Version;

        listing.PlaceBid(Ann, 12.00m, Now);

        Assert.Equal(before + 1, listing.Version);
    }

    [Fact]
    public void The_seller_the_highest_bidder_and_late_bids_are_refused()
    {
        var listing = Running();
        listing.PlaceBid(Ann, 12.00m, Now);

        Assert.Equal(BidRefusal.OwnListing, listing.PlaceBid(Seller, 20m, Now).Refusal);
        Assert.Equal(BidRefusal.AlreadyHighest, listing.PlaceBid(Ann, 20m, Now).Refusal);
        Assert.Equal(BidRefusal.Ended, listing.PlaceBid(Bea, 20m, listing.AuctionEndsAt!.Value).Refusal);
    }

    [Fact]
    public void A_fixed_price_listing_takes_no_bids()
    {
        var listing = Listing.CreateDraft(Seller, 10001, Details(format: "FixedPrice"), Now);
        listing.Publish(Now, photoCount: 1);

        Assert.Equal(BidRefusal.NotAnAuction, listing.PlaceBid(Ann, 20m, Now).Refusal);
    }

    [Fact]
    public void Closing_with_bids_reserves_it_for_the_winner_for_48_hours()
    {
        var listing = Running();
        listing.PlaceBid(Ann, 12.00m, Now);
        listing.PlaceBid(Bea, 18.00m, Now);
        var end = listing.AuctionEndsAt!.Value;

        Assert.Null(listing.Close(end.AddSeconds(-1)));
        Assert.Equal(AuctionOutcome.Won, listing.Close(end));

        Assert.Equal(ListingStatus.Reserved, listing.Status);
        Assert.Equal(Bea, listing.WinnerId);
        Assert.Equal(end.AddHours(48), listing.PayBy);
        Assert.Equal(listing.PayBy, listing.ReservedUntil);
        Assert.Null(listing.ReservedByCheckoutId);
        Assert.Null(listing.Close(end.AddMinutes(1)));
    }

    [Fact]
    public void Closing_without_bids_removes_it_as_unsold_and_relisting_starts_a_new_run()
    {
        var listing = Running(days: 3);
        var end = listing.AuctionEndsAt!.Value;

        Assert.Equal(AuctionOutcome.Unsold, listing.Close(end));
        Assert.Equal(ListingStatus.Removed, listing.Status);
        Assert.Equal(ListingRemovalReason.AuctionUnsold, listing.RemovalReason);
        Assert.True(listing.CanRelist);

        var later = end.AddDays(2);
        listing.Relist(later);

        Assert.Equal(ListingStatus.Active, listing.Status);
        Assert.Equal(later.AddDays(3), listing.AuctionEndsAt);
        Assert.Equal(0, listing.BidCount);
        Assert.Null(listing.CurrentBid);
        Assert.Null(listing.HighBidderId);
    }

    [Fact]
    public void A_lapsed_winner_checkout_keeps_the_hold_until_the_payment_window_ends()
    {
        var listing = Running();
        listing.PlaceBid(Ann, 15.00m, Now);
        var end = listing.AuctionEndsAt!.Value;
        listing.Close(end);
        var checkoutId = Guid.NewGuid();
        listing.HoldForCheckout(checkoutId, end.AddMinutes(10));

        listing.ReleaseReservation(checkoutId, end.AddMinutes(10));

        Assert.Equal(ListingStatus.Reserved, listing.Status);
        Assert.Null(listing.ReservedByCheckoutId);
        Assert.Equal(listing.PayBy, listing.ReservedUntil);
        Assert.Equal(Ann, listing.WinnerId);
    }

    [Fact]
    public void An_unpaid_window_removes_it_as_unpaid_unless_a_checkout_holds_it()
    {
        var listing = Running();
        listing.PlaceBid(Ann, 15.00m, Now);
        listing.Close(listing.AuctionEndsAt!.Value);
        var payBy = listing.PayBy!.Value;

        Assert.False(listing.ExpirePayment(payBy.AddSeconds(-1)));
        listing.HoldForCheckout(Guid.NewGuid(), payBy.AddMinutes(5));
        Assert.False(listing.ExpirePayment(payBy));

        Assert.True(listing.ExpirePayment(payBy.AddMinutes(5)));
        Assert.Equal(ListingStatus.Removed, listing.Status);
        Assert.Equal(ListingRemovalReason.AuctionUnpaid, listing.RemovalReason);
        Assert.True(listing.CanRelist);
    }

    [Fact]
    public void With_bids_an_auction_cannot_be_edited_or_withdrawn()
    {
        var listing = Running();
        Assert.Null(listing.EditRefusal);

        listing.PlaceBid(Ann, 12.00m, Now);

        Assert.Equal("AUCTION_HAS_BIDS", listing.EditRefusal);
        Assert.Throws<InvalidOperationException>(() => listing.Withdraw(Now));
    }

    [Fact]
    public void An_edit_keeps_the_format_and_length_but_can_change_the_starting_bid()
    {
        var listing = Running(days: 5);
        var end = listing.AuctionEndsAt;

        Assert.Empty(listing.Edit(Details(format: "FixedPrice", days: 7, price: 20.00m), photoCount: 1, Now));

        Assert.Equal(ListingFormat.Auction, listing.Format);
        Assert.Equal(5, listing.AuctionDays);
        Assert.Equal(end, listing.AuctionEndsAt);
        Assert.Equal(20.00m, listing.MinimumBid);
    }

    [Fact]
    public void An_auction_cannot_be_a_showcase_listing()
    {
        var listing = Running();

        Assert.False(listing.CanBeShowcase);
        Assert.Throws<InvalidOperationException>(() => listing.SetShowcase(true, Now));
    }

    [Fact]
    public void A_bid_records_its_bidder_and_amount()
    {
        var listing = Running();

        var bid = listing.PlaceBid(Ann, 12.00m, Now).Bid!;

        Assert.Equal((listing.Id, Ann, 12.00m, Now), (bid.ListingId, bid.BidderId, bid.Amount, bid.PlacedAt));
    }
}
