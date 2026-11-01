// Acceptance Test
// Traces to: L2-075, L2-113
// Description: Verify seed publishes the newest demo listings as 7-day auctions that are still
// running after the seed, with no bids, through the same create and publish commands.

using TuTuMuch.Cli.Tests.Support;
using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Cli.Tests;

public sealed class SeedAuctionsTests(SeededDemo demo) : IClassFixture<SeededDemo>
{
    [Fact]
    public void The_newest_listings_are_running_auctions_without_bids()
    {
        var auctions = demo.Harness.InDatabase(db => db.Listings.Where(l => l.Format == ListingFormat.Auction).ToList());

        Assert.Equal(DemoCatalogue.AuctionCount, auctions.Count);
        Assert.All(auctions, a =>
        {
            Assert.Equal((ListingStatus.Active, 7, 0), (a.Status, a.AuctionDays, a.BidCount));
            Assert.True(a.AuctionEndsAt > DateTime.UtcNow, "The demo auction should still be running");
        });
    }
}
