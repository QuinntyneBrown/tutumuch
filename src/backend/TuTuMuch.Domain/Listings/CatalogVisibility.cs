using System.Linq.Expressions;

namespace TuTuMuch.Domain.Listings;

/// <summary>The one rule for which listings the catalog shows: Active and Reserved (L2-022).</summary>
public static class CatalogVisibility
{
    public static readonly Expression<Func<Listing, bool>> IsVisible =
        l => l.Status == ListingStatus.Active || l.Status == ListingStatus.Reserved;
}
