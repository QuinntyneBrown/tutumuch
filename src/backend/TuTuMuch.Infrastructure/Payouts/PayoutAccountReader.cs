using Microsoft.EntityFrameworkCore;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Domain.Payouts;

namespace TuTuMuch.Infrastructure.Payouts;

/// <summary>A seller is onboarded when the provider reported their connected account Complete (L2-031 AC1).</summary>
internal sealed class PayoutAccountReader(ITuTuMuchDbContext db) : ISellerPayoutAccountReader
{
    public Task<bool> IsOnboardedAsync(Guid sellerId, CancellationToken cancellationToken) =>
        db.SellerPayoutAccounts.AnyAsync(a => a.MemberId == sellerId && a.Status == PayoutAccountStatus.Complete, cancellationToken);
}
