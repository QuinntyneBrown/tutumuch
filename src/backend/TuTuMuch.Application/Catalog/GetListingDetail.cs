using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Common;
using TuTuMuch.Application.Listings;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Application.Catalog;

/// <summary>
/// The public summary of a seller: completed sales as seller, and no rating while review
/// authoring is deferred (ADR reviews/0001), the same figures as the member profile.
/// </summary>
public sealed record SellerSummaryDto(string DisplayName, int CompletedSalesCount, decimal? RatingAverage, int PublishedReviewCount);

/// <summary>Anonymous, Seller or OtherMember: decides whether the page offers Buy now (L2-026).</summary>
public sealed record ViewerDto(string Relation);

/// <summary>
/// Everything the detail page shows. Money is two-place decimal strings (L2-018); no email or real
/// name (L2-054). Showcase tells the page that Buy now shows the Sold notice (L2-108); nothing says
/// who requested it (L2-111).
/// </summary>
public sealed record ListingDetailDto(
    Guid Id,
    string Reference,
    string Title,
    string Brand,
    string Category,
    string? Subcategory,
    string SizeLabel,
    string Condition,
    string? Flaws,
    string Description,
    string Price,
    string ShippingCost,
    string Total,
    string Status,
    DateTime PublishedAt,
    IReadOnlyList<CardPhotoDto> Photos,
    SellerSummaryDto Seller,
    ViewerDto Viewer,
    bool Showcase);

/// <summary>One listing by its reference, as seen by <paramref name="ViewerId"/> (null for a visitor).</summary>
public sealed record GetListingDetailQuery(string Reference, Guid? ViewerId) : IRequest<Result<ListingDetailDto>>;

/// <summary>
/// Hides Draft and Removed listings from everyone but the seller with the same 404 as an unknown
/// reference (<see cref="ListingDetailVisibility"/>), and reports how the viewer relates to it.
/// </summary>
public sealed class GetListingDetailQueryHandler(ITuTuMuchDbContext db, IOptions<PhotoOptions> photoOptions) :
    IRequestHandler<GetListingDetailQuery, Result<ListingDetailDto>>
{
    public async Task<Result<ListingDetailDto>> Handle(GetListingDetailQuery query, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.AsNoTracking().SingleOrDefaultAsync(l => l.Reference == query.Reference, cancellationToken);
        if (listing is null || !ListingDetailVisibility.CanView(listing.Status, listing.SellerId, query.ViewerId))
        {
            return Error.NotFound("LISTING_NOT_FOUND");
        }

        var completedSales = await db.Orders.CountAsync(o => o.SellerId == listing.SellerId && o.Status == OrderStatus.Completed, cancellationToken);
        var sellerName = await db.Members.AsNoTracking().Where(m => m.Id == listing.SellerId).Select(m => m.DisplayName).SingleAsync(cancellationToken);
        var photos = await db.ListingPhotos.AsNoTracking()
            .Where(p => p.ListingId == listing.Id).OrderBy(p => p.Position)
            .ToListAsync(cancellationToken);
        var baseUrl = photoOptions.Value.PublicBaseUrl.TrimEnd('/');
        var price = listing.Price ?? 0m;
        var shipping = listing.ShippingCost ?? 0m;
        var relation = query.ViewerId is null ? "Anonymous" : query.ViewerId == listing.SellerId ? "Seller" : "OtherMember";
        return new ListingDetailDto(
            listing.Id,
            listing.Reference,
            listing.Title ?? string.Empty,
            listing.Brand ?? string.Empty,
            listing.Category,
            listing.Subcategory,
            CategoryCatalog.Find(listing.Category)?.SizeLabel ?? listing.Size,
            listing.Condition?.ToLabel() ?? string.Empty,
            listing.Flaws,
            listing.Description ?? string.Empty,
            Money(price),
            Money(shipping),
            Money(price + shipping),
            listing.Status.ToString(),
            listing.PublishedAt ?? listing.CreatedAt,
            photos.Select((p, i) => CatalogPhotos.ToDto(p, $"Photo {i + 1} of {listing.Brand} {listing.Title}", baseUrl)).ToList(),
            new SellerSummaryDto(sellerName, completedSales, null, 0),
            new ViewerDto(relation),
            listing.IsShowcase);
    }

    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
