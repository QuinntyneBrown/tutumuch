using System.Globalization;
using TuTuMuch.Application.Catalog;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Application.Checkouts;

/// <summary>The item being bought, as the order summary shows it.</summary>
public sealed record CheckoutItemDto(string Reference, string Title, string Brand, string Size, string Condition, CardPhotoDto? Photo);

/// <summary>A US shipping address as the API reads and writes it. Country is the two-letter code.</summary>
public sealed record AddressDto(string FullName, string Line1, string? Line2, string City, string State, string Zip, string Country)
{
    public static AddressDto From(ShippingAddress a) => new(a.FullName, a.Line1, a.Line2, a.City, a.State, a.Zip, a.Country);
}

/// <summary>
/// One checkout as its buyer sees it. Money is two-place decimal strings (L2-018); tax and total
/// are null until an address is set. Status is Expired as soon as the 10 minutes have passed.
/// </summary>
public sealed record CheckoutDto(
    Guid Id,
    string Status,
    DateTime ExpiresAt,
    CheckoutItemDto Item,
    string ItemPrice,
    string ShippingCost,
    string? SalesTax,
    string? Total,
    AddressDto? Address,
    AddressDto? SavedAddress,
    string? LastDeclineReason,
    Guid? OrderId);

/// <summary>A payment attempt accepted for processing; the result arrives by webhook.</summary>
public sealed record PaymentDto(Guid Id, string Status);

internal static class CheckoutMoney
{
    public static string Format(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    public static string? Format(decimal? value) => value is { } v ? Format(v) : null;
}

internal static class CheckoutViews
{
    public static CheckoutDto ToDto(Checkout checkout, Listing listing, CardPhotoDto? photo, ShippingAddress? saved, DateTime now)
    {
        var status = checkout.HoldsListing && !checkout.IsLive(now) ? CheckoutStatus.Expired : checkout.Status;
        return new CheckoutDto(
            checkout.Id,
            status.ToString(),
            checkout.ExpiresAt,
            new CheckoutItemDto(listing.Reference, listing.Title ?? string.Empty, listing.Brand ?? string.Empty, listing.Size, listing.Condition?.ToLabel() ?? string.Empty, photo),
            CheckoutMoney.Format(checkout.ItemPrice),
            CheckoutMoney.Format(checkout.ShippingCost),
            CheckoutMoney.Format(checkout.SalesTax),
            CheckoutMoney.Format(checkout.Total),
            checkout.Address is null ? null : AddressDto.From(checkout.Address),
            saved is null ? null : AddressDto.From(saved),
            checkout.LastDeclineReason,
            checkout.OrderId);
    }
}
