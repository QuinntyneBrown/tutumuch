using MediatR;
using Microsoft.EntityFrameworkCore;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Application.Checkouts;

/// <summary>Ends every checkout whose 10 minutes have passed and puts its listing back on sale (L2-028 AC3). Returns how many ended.</summary>
public sealed record ExpireReservationsCommand : IRequest<int>;

public sealed class ExpireReservationsHandler(ITuTuMuchDbContext db, TimeProvider clock) : IRequestHandler<ExpireReservationsCommand, int>
{
    public async Task<int> Handle(ExpireReservationsCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var lapsed = await db.Checkouts
            .Where(c => (c.Status == CheckoutStatus.Open || c.Status == CheckoutStatus.Processing) && c.ExpiresAt <= now)
            .ToListAsync(cancellationToken);
        if (lapsed.Count == 0)
        {
            return 0;
        }

        var listingIds = lapsed.Select(c => c.ListingId).ToList();
        var listings = await db.Listings.Where(l => listingIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, cancellationToken);
        foreach (var checkout in lapsed)
        {
            checkout.Release(CheckoutStatus.Expired, now);
            listings[checkout.ListingId].ReleaseReservation(checkout.Id, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        return lapsed.Count;
    }
}
