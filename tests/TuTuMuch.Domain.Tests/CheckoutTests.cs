// Unit Test
// Traces to: L2-026, L2-027, L2-028, L2-030
// Description: The checkout lifecycle, US address rules and the listing's reservation transitions.

using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Domain.Tests;

public sealed class CheckoutTests
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    private static readonly ShippingAddress Brooklyn = ShippingAddress.Create(" Marcus Bell ", "214 Kent Ave", " ", "Brooklyn", "ny", "11249");

    [Fact]
    public void A_checkout_holds_its_listing_for_10_minutes()
    {
        var checkout = Checkout.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 85m, 9m, Now);

        Assert.Equal(Now.AddMinutes(10), checkout.ExpiresAt);
        Assert.True(checkout.IsLive(Now.AddMinutes(9).AddSeconds(59)));
        Assert.False(checkout.IsLive(Now.AddMinutes(10)));
    }

    [Fact]
    public void The_total_is_item_plus_shipping_plus_tax()
    {
        var checkout = Checkout.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 85m, 9m, Now);

        checkout.SetAddress(Brooklyn, 8.34m);

        Assert.Equal(102.34m, checkout.Total);
    }

    [Fact]
    public void A_decline_returns_a_processing_checkout_to_open_with_the_reason()
    {
        var checkout = Checkout.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 85m, 9m, Now);
        checkout.SetAddress(Brooklyn, 8.34m);
        checkout.BeginPayment();

        checkout.RecordDecline("Insufficient funds.");

        Assert.Equal(CheckoutStatus.Open, checkout.Status);
        Assert.Equal("Insufficient funds.", checkout.LastDeclineReason);
    }

    [Fact]
    public void Releasing_a_paid_checkout_changes_nothing()
    {
        var checkout = Checkout.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 85m, 9m, Now);
        checkout.MarkPaid(Guid.NewGuid(), Now);

        checkout.Release(CheckoutStatus.Expired, Now);

        Assert.Equal(CheckoutStatus.Paid, checkout.Status);
    }

    [Fact]
    public void An_address_is_trimmed_and_its_state_upper_cased()
    {
        Assert.Equal(new ShippingAddress("Marcus Bell", "214 Kent Ave", null, "Brooklyn", "NY", "11249", "US"), Brooklyn);
    }

    [Theory]
    [InlineData("US", false)]
    [InlineData("us", false)]
    [InlineData(null, false)]
    [InlineData("CA", true)]
    [InlineData("MX", true)]
    public void Only_the_united_states_is_accepted(string? country, bool outside)
    {
        Assert.Equal(outside, ShippingAddress.IsOutsideUnitedStates(country));
    }

    [Fact]
    public void Every_missing_field_of_an_address_is_reported()
    {
        var violations = ShippingAddress.Check(null, "", null, " ", "ZZ", "1124");

        Assert.Equal(["fullName", "line1", "city", "state", "zip"], violations.Select(v => v.Field));
    }

    [Theory]
    [InlineData("11249", true)]
    [InlineData("11249-1234", true)]
    [InlineData("1124", false)]
    [InlineData("M5H 2M9", false)]
    [InlineData("11249-12", false)]
    public void A_zip_code_has_5_digits_and_an_optional_plus_four(string zip, bool valid)
    {
        Assert.Equal(valid, ShippingAddress.Check("Marcus Bell", "214 Kent Ave", null, "Brooklyn", "NY", zip).Count == 0);
    }

    [Fact]
    public void A_seller_cannot_buy_their_own_listing()
    {
        var sellerId = Guid.NewGuid();
        var listing = Listing.CreateDraft(sellerId, 10001, new ListingDetails("Dresses", null, null, null, null, null, null, null, null, null), Now);

        Assert.False(listing.CanBePurchasedBy(sellerId));
        Assert.True(listing.CanBePurchasedBy(Guid.NewGuid()));
    }

    [Fact]
    public void Only_a_reserved_or_active_listing_can_be_sold()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), 10001, new ListingDetails("Dresses", null, null, null, null, null, null, null, null, null), Now);

        Assert.Throws<InvalidOperationException>(() => listing.MarkSold(Now));
    }
}
