using MediatR;
using Microsoft.EntityFrameworkCore;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Common;
using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Application.Listings;

/// <summary>
/// The refusals of edit and withdraw (L2-020): a Reserved, Sold or withdrawn listing cannot change,
/// and an edit needs the version it was read at.
/// </summary>
public static class ListingRefusals
{
    public static readonly Error Reserved = new(409, "LISTING_RESERVED", "This item is in someone's checkout");
    public static readonly Error Sold = new(409, "LISTING_SOLD", "This item has sold");
    public static readonly Error NotEditable = new(409, "LISTING_NOT_EDITABLE", "This listing was withdrawn");
    public static readonly Error NotWithdrawable = new(409, "LISTING_NOT_EDITABLE", "Only an active listing can be withdrawn");
    public static readonly Error Changed = new(412, "LISTING_CHANGED", "This listing changed since you opened it");
    public static readonly Error PreconditionRequired = new(428, "PRECONDITION_REQUIRED", "Send the version you read in If-Match");

    public static Error? For(Listing listing) => listing.EditRefusal switch
    {
        "LISTING_RESERVED" => Reserved,
        "LISTING_SOLD" => Sold,
        "LISTING_NOT_EDITABLE" => NotEditable,
        _ => null,
    };

    /// <summary>After a lost concurrency race: refused as reserved or sold when a checkout won, otherwise as changed.</summary>
    public static async Task<Error> AfterConflictAsync(ITuTuMuchDbContext db, Guid listingId, CancellationToken cancellationToken)
    {
        var status = await db.Listings.AsNoTracking().Where(l => l.Id == listingId).Select(l => l.Status).SingleAsync(cancellationToken);
        return status switch
        {
            ListingStatus.Reserved => Reserved,
            ListingStatus.Sold => Sold,
            _ => Changed,
        };
    }
}

public sealed record WithdrawListingResult(string Reference, string Status);

/// <summary>
/// The seller withdraws an Active listing: Removed with reason Withdrawn, out of the catalog and
/// search for good (L2-020). Repeating it on a withdrawn listing answers 200. 404 for anyone but the
/// seller; 409 for a Reserved, Sold or draft listing, or one removed after a cancelled order.
/// </summary>
public sealed record WithdrawListingCommand(Guid SellerId, string Reference) : IRequest<Result<WithdrawListingResult>>;

public sealed class WithdrawListingCommandHandler(ITuTuMuchDbContext db, TimeProvider clock) : IRequestHandler<WithdrawListingCommand, Result<WithdrawListingResult>>
{
    public async Task<Result<WithdrawListingResult>> Handle(WithdrawListingCommand command, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.SingleOrDefaultAsync(l => l.Reference == command.Reference && l.SellerId == command.SellerId, cancellationToken);
        if (listing is null)
        {
            return Error.NotFound("LISTING_NOT_FOUND");
        }

        if (listing.Status is ListingStatus.Reserved or ListingStatus.Sold)
        {
            return ListingRefusals.For(listing)!;
        }

        var withdrawn = listing.Status == ListingStatus.Removed && listing.RemovalReason == ListingRemovalReason.Withdrawn;
        if (!withdrawn && listing.Status != ListingStatus.Active)
        {
            return ListingRefusals.NotWithdrawable;
        }

        if (!withdrawn)
        {
            listing.Withdraw(clock.GetUtcNow().UtcDateTime);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return await ListingRefusals.AfterConflictAsync(db, listing.Id, cancellationToken);
            }
        }

        return new WithdrawListingResult(listing.Reference, listing.Status.ToString());
    }
}
