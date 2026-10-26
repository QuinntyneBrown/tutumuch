using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Catalog;
using TuTuMuch.Application.Common;
using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Application.Listings;

/// <summary>
/// One row of "My listings" (L2-021): the cover, the title (null for an untitled draft, which the
/// page names by its category), the price, the status and the date first saved, plus the order of a
/// Sold listing so the row can open the sale.
/// </summary>
public sealed record MyListingRowDto(string Reference, string Status, string? Title, string Category, decimal? Price, CardPhotoDto? Photo, DateTime CreatedAt, Guid? SaleOrderId);

/// <summary>How many of the seller's listings are in each status; the chips of the status filter read them.</summary>
public sealed record MyListingCountsDto(int All, int Draft, int Active, int Reserved, int Sold, int Removed);

public sealed record MyListingsDto(IReadOnlyList<MyListingRowDto> Items, MyListingCountsDto Counts);

/// <summary>
/// The signed-in seller's listings in every status, or in <see cref="Status"/> only, newest first
/// (CreatedAt, then Id). The counts always cover every status. Every listing is returned: paging
/// waits until a seller can have more listings than one page shows (detailed design
/// manage-my-listings, open questions).
/// </summary>
public sealed record GetMyListingsQuery(Guid SellerId, string? Status) : IRequest<Result<MyListingsDto>>;

public sealed class GetMyListingsQueryHandler(ITuTuMuchDbContext db, IOptions<PhotoOptions> photoOptions) : IRequestHandler<GetMyListingsQuery, Result<MyListingsDto>>
{
    public async Task<Result<MyListingsDto>> Handle(GetMyListingsQuery query, CancellationToken cancellationToken)
    {
        ListingStatus? status = null;
        if (query.Status is not null)
        {
            if (!Enum.TryParse<ListingStatus>(query.Status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            {
                return new Error(400, "VALIDATION_FAILED", "Choose Draft, Active, Reserved, Sold or Removed");
            }

            status = parsed;
        }

        var mine = db.Listings.AsNoTracking().Where(l => l.SellerId == query.SellerId);
        var counts = await mine.GroupBy(l => l.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        int Count(ListingStatus s) => counts.SingleOrDefault(c => c.Status == s)?.Count ?? 0;

        var rows = await (status is { } only ? mine.Where(l => l.Status == only) : mine)
            .OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id)
            .Select(l => new { l.Id, l.Reference, l.Status, l.Title, l.Category, l.Price, l.CreatedAt })
            .ToListAsync(cancellationToken);
        var ids = rows.Select(r => r.Id).ToList();
        var covers = (await db.ListingPhotos.AsNoTracking()
                .Where(p => p.ListingId != null && ids.Contains(p.ListingId.Value))
                .ToListAsync(cancellationToken))
            .GroupBy(p => p.ListingId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Position).First());
        var soldIds = rows.Where(r => r.Status == ListingStatus.Sold).Select(r => r.Id).ToList();
        var orders = await db.Orders.AsNoTracking()
            .Where(o => soldIds.Contains(o.ListingId))
            .ToDictionaryAsync(o => o.ListingId, o => o.Id, cancellationToken);
        var baseUrl = photoOptions.Value.PublicBaseUrl.TrimEnd('/');

        var items = rows.Select(r => new MyListingRowDto(
            r.Reference,
            r.Status.ToString(),
            r.Title,
            r.Category,
            r.Price,
            covers.TryGetValue(r.Id, out var cover) ? CatalogPhotos.ToDto(cover, string.Empty, baseUrl) : null,
            r.CreatedAt,
            orders.TryGetValue(r.Id, out var orderId) ? orderId : null)).ToList();

        return new MyListingsDto(items, new MyListingCountsDto(
            counts.Sum(c => c.Count),
            Count(ListingStatus.Draft),
            Count(ListingStatus.Active),
            Count(ListingStatus.Reserved),
            Count(ListingStatus.Sold),
            Count(ListingStatus.Removed)));
    }
}
