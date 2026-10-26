using MediatR;
using Microsoft.EntityFrameworkCore;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Common;

namespace TuTuMuch.Application.Listings;

/// <summary>
/// The seller relists the listing of a cancelled order: Removed → Active with its original photos,
/// price and description, back in the catalog (L2-035 AC4). 404 for anyone but its seller; 409
/// LISTING_NOT_RELISTABLE for any listing not removed by a cancellation.
/// </summary>
public sealed record RelistListingCommand(Guid SellerId, string Reference) : IRequest<Result<bool>>;

public sealed class RelistListingCommandHandler(ITuTuMuchDbContext db, TimeProvider clock) : IRequestHandler<RelistListingCommand, Result<bool>>
{
    public static readonly Error NotRelistable = new(409, "LISTING_NOT_RELISTABLE", "Only the listing of a cancelled order can be relisted");

    public async Task<Result<bool>> Handle(RelistListingCommand command, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.SingleOrDefaultAsync(l => l.Reference == command.Reference && l.SellerId == command.SellerId, cancellationToken);
        if (listing is null)
        {
            return Error.NotFound("LISTING_NOT_FOUND");
        }

        if (!listing.CanRelist)
        {
            return NotRelistable;
        }

        listing.Relist(clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
