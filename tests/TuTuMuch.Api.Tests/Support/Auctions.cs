using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TuTuMuch.Application.Listings;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Members;

namespace TuTuMuch.Api.Tests.Support;

/// <summary>Seeds auctions and members and drives the bid and close endpoints (L2-113 to L2-120).</summary>
public static class Auctions
{
    /// <summary>An Active auction of a pink tutu with a $12.00 starting bid and $5.00 shipping, published now by the factory clock.</summary>
    public static string SeedAuction(this TuTuMuchApiFactory factory, Guid sellerId, long number = 40001, int days = 5, decimal startingBid = 12.00m)
    {
        var reference = Listing.ReferenceFor(number);
        var now = factory.Clock.GetUtcNow().UtcDateTime;
        factory.InDatabase(db =>
        {
            var details = new ListingDetails("Dance", "Tutus", null, "Rainbow tulle tutu, pink", "Light wear, colours still bright.", "Capezio", "Used - Good", null, startingBid, 5.00m, "Auction", days);
            var listing = Listing.CreateDraft(sellerId, number, details, now);
            Assert.Empty(listing.Publish(now, photoCount: 1));
            db.Listings.Add(listing);
            db.SaveChanges();
        });
        return reference;
    }

    /// <summary>A verified member, signed in on a new client by a minted session cookie.</summary>
    public static (Member Member, HttpClient Client) VerifiedClient(this TuTuMuchApiFactory factory, string name)
    {
        var member = Member.Register(name, $"{name}@example.com", "hash", DateTime.UtcNow);
        member.MarkEmailVerified(DateTime.UtcNow);
        factory.InDatabase(db =>
        {
            db.Members.Add(member);
            db.SaveChanges();
        });
        return (member, factory.CreateMemberClient().WithSession(factory, member));
    }

    public static Task<HttpResponseMessage> BidAsync(this HttpClient client, string reference, decimal amount) =>
        client.PostAsJsonAsync($"/api/listings/{reference}/bids", new { amount });

    /// <summary>One pass of the worker's AuctionCloseJob.</summary>
    public static async Task<int> CloseAuctionsAsync(this TuTuMuchApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CloseAuctionsCommand());
    }

    /// <summary>One pass of the worker's ReservationExpiryJob.</summary>
    public static async Task<int> ExpireReservationsAsync(this TuTuMuchApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new TuTuMuch.Application.Checkouts.ExpireReservationsCommand());
    }

    public static Listing ListingOf(this TuTuMuchApiFactory factory, string reference)
    {
        Listing? listing = null;
        factory.InDatabase(db => listing = db.Listings.Single(l => l.Reference == reference));
        return listing!;
    }
}

public sealed record AuctionView(
    string CurrentPrice,
    string StartingBid,
    int BidCount,
    DateTime EndsAt,
    string MinimumBid,
    bool Ended,
    bool ViewerIsHighestBidder,
    bool ViewerHasBid,
    bool ViewerIsWinner,
    DateTime? PayBy);

public sealed record AuctionDetailView(string Reference, string Price, string Status, string Format, AuctionView? Auction);
