using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Common;
using TuTuMuch.Application.Notifications;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Notifications;

namespace TuTuMuch.Application.Listings;

/// <summary>
/// An auction as one viewer sees it (L2-114): money as two-place strings (L2-018), and only the
/// viewer's own standing, never another bidder (L2-120). PayBy is set while the auction waits for
/// its winner to pay.
/// </summary>
public sealed record AuctionViewDto(
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

public static class AuctionViews
{
    /// <summary>The auction view of <paramref name="listing"/>, or null for a fixed-price listing.</summary>
    public static AuctionViewDto? For(Listing listing, Guid? viewerId, bool viewerHasBid, DateTime now) =>
        !listing.IsAuction || listing.AuctionEndsAt is not { } endsAt
            ? null
            : new AuctionViewDto(
                Money(listing.CurrentPrice ?? 0m),
                Money(listing.Price ?? 0m),
                listing.BidCount,
                endsAt,
                Money(listing.MinimumBid ?? 0m),
                !listing.IsRunning(now),
                viewerId is not null && viewerId == listing.HighBidderId && listing.IsRunning(now),
                viewerHasBid,
                viewerId is not null && listing.IsAwaitingWinner && viewerId == listing.WinnerId,
                listing.IsAwaitingWinner ? listing.PayBy : null);

    /// <summary>Whether the viewer bid in the auction's current run, which started its length before its end.</summary>
    public static async Task<bool> ViewerHasBidAsync(ITuTuMuchDbContext db, Listing listing, Guid? viewerId, CancellationToken cancellationToken)
    {
        if (viewerId is not { } viewer || !listing.IsAuction || listing.AuctionEndsAt is not { } endsAt)
        {
            return false;
        }

        var runStart = endsAt.AddDays(-(listing.AuctionDays ?? 0));
        return await db.Bids.AnyAsync(b => b.ListingId == listing.Id && b.BidderId == viewer && b.PlacedAt >= runStart, cancellationToken);
    }

    public static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}

/// <summary>A verified member bids on an Active auction (L2-115); the version token decides races (L2-116).</summary>
public sealed record PlaceBidCommand(Guid BidderId, string Reference, decimal Amount) : IRequest<Result<AuctionViewDto>>;

public sealed class PlaceBidCommandHandler(ITuTuMuchDbContext db, IEmailOutbox outbox, TimeProvider clock) : IRequestHandler<PlaceBidCommand, Result<AuctionViewDto>>
{
    public static readonly Error NotAnAuction = new(409, "NOT_AN_AUCTION", "This item has a fixed price");
    public static readonly Error Ended = new(409, "AUCTION_ENDED", "This auction has ended");
    public static readonly Error OwnListing = new(422, "CANNOT_BID_OWN_LISTING", "You can't bid on your own auction");
    public static readonly Error AlreadyHighest = new(409, "ALREADY_HIGHEST_BIDDER", "You're already the highest bidder");

    public async Task<Result<AuctionViewDto>> Handle(PlaceBidCommand command, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.SingleOrDefaultAsync(l => l.Reference == command.Reference, cancellationToken);
        if (listing is null || !ListingDetailVisibility.CanView(listing.Status, listing.SellerId, command.BidderId))
        {
            return Error.NotFound("LISTING_NOT_FOUND");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var minimum = listing.MinimumBid ?? 0m;
        var result = listing.PlaceBid(command.BidderId, command.Amount, now);
        if (result.Refusal is { } refusal)
        {
            return refusal switch
            {
                BidRefusal.NotAnAuction => NotAnAuction,
                BidRefusal.Ended => Ended,
                BidRefusal.OwnListing => OwnListing,
                BidRefusal.AlreadyHighest => AlreadyHighest,
                BidRefusal.TooLow => AmountError("BID_TOO_LOW", $"Bid ${AuctionViews.Money(minimum)} or more"),
                _ => AmountError("BID_INVALID", $"Enter a bid from ${AuctionViews.Money(minimum)} to $5,000.00"),
            };
        }

        db.Bids.Add(result.Bid!);
        if (result.Outbid is { } outbid)
        {
            outbox.Enqueue(NotificationType.Outbid, outbid, new OutbidEmail(listing.Reference, listing.Title ?? listing.Reference, listing.CurrentBid!.Value, listing.AuctionEndsAt!.Value));
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another bid, or an edit, was saved against the version this bid read (L2-116).
            var latest = await db.Listings.AsNoTracking().SingleAsync(l => l.Id == listing.Id, cancellationToken);
            return latest.IsRunning(now)
                ? new Error(409, "OUTBID", "Someone just bid higher", new Dictionary<string, FieldError>
                {
                    ["amount"] = new("OUTBID", $"Bid ${AuctionViews.Money(latest.MinimumBid ?? 0m)} or more"),
                })
                : Ended;
        }

        return AuctionViews.For(listing, command.BidderId, viewerHasBid: true, now)!;
    }

    private static Error AmountError(string code, string message) =>
        Error.Invalid(code, new Dictionary<string, FieldError> { ["amount"] = new(code, message) }, message);
}

/// <summary>
/// Closes every Active auction whose end time has passed (L2-117) and ends every payment window
/// that passed unpaid (L2-118), queuing the emails of L2-119 with each change. Returns how many
/// listings changed.
/// </summary>
public sealed record CloseAuctionsCommand : IRequest<int>;

public sealed class CloseAuctionsCommandHandler(ITuTuMuchDbContext db, IEmailOutbox outbox, TimeProvider clock) : IRequestHandler<CloseAuctionsCommand, int>
{
    public async Task<int> Handle(CloseAuctionsCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var due = await db.Listings
            .Where(l => l.Format == ListingFormat.Auction
                && ((l.Status == ListingStatus.Active && l.AuctionEndsAt <= now)
                    || (l.Status == ListingStatus.Reserved && l.WinnerId != null && l.PayBy <= now)))
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);
        var changed = 0;
        foreach (var id in due)
        {
            // One listing per save, so a listing that lost a race does not hold back the others.
            var listing = await db.Listings.SingleAsync(l => l.Id == id, cancellationToken);
            var title = listing.Title ?? listing.Reference;
            if (listing.Close(now) is { } outcome)
            {
                outbox.Enqueue(NotificationType.AuctionEnded, listing.SellerId, new AuctionEndedEmail(listing.Reference, title, outcome == AuctionOutcome.Won ? listing.CurrentBid : null));
                if (outcome == AuctionOutcome.Won)
                {
                    outbox.Enqueue(NotificationType.AuctionWon, listing.WinnerId!.Value, new AuctionWonEmail(listing.Reference, title, listing.CurrentBid!.Value, listing.PayBy!.Value));
                }
            }
            else
            {
                var winner = listing.WinnerId;
                if (!listing.ExpirePayment(now))
                {
                    continue;
                }

                outbox.Enqueue(NotificationType.AuctionPaymentMissed, winner!.Value, new AuctionPaymentMissedEmail(listing.Reference, title, ToSeller: false));
                outbox.Enqueue(NotificationType.AuctionPaymentMissed, listing.SellerId, new AuctionPaymentMissedEmail(listing.Reference, title, ToSeller: true));
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                changed++;
            }
            catch (DbUpdateConcurrencyException)
            {
                // A bid or checkout changed it after it was read; the next run looks again.
                db.ChangeTracker.Clear();
            }
        }

        return changed;
    }
}
