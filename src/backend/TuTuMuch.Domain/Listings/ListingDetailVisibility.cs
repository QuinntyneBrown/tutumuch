namespace TuTuMuch.Domain.Listings;

/// <summary>
/// Who may open a listing's detail page (L2-025): anyone for Active, Reserved and Sold; only the
/// seller for Draft and Removed.
/// </summary>
public static class ListingDetailVisibility
{
    public static bool CanView(ListingStatus status, Guid sellerId, Guid? viewerId) =>
        status is ListingStatus.Active or ListingStatus.Reserved or ListingStatus.Sold || viewerId == sellerId;
}
