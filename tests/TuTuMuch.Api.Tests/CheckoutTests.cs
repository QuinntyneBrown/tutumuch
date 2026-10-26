// Acceptance Test
// Traces to: L2-026, L2-027, L2-028, L2-029
// Description: A verified buyer starts checkout and the listing is Reserved for them for 10
// minutes; nobody else can start checkout meanwhile, the seller can't buy their own listing, and
// the reservation ends when the buyer cancels or the 10 minutes pass. Checkout takes a US
// address only, reports every missing field and prices tax on item plus shipping.

using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TuTuMuch.Api.Tests.Support;
using TuTuMuch.Application.Checkouts;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Members;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Api.Tests;

public sealed class CheckoutTests : IDisposable
{
    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _buyer;
    private readonly HttpClient _other;

    public CheckoutTests()
    {
        _buyer = _factory.CreateMemberClient();
        _other = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task Starting_checkout_reserves_the_listing_for_10_minutes_and_prices_it()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var reference = _factory.SeedActiveListing();
        var now = _factory.Clock.GetUtcNow().UtcDateTime;

        using var response = await _buyer.StartCheckoutAsync(reference);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var checkout = (await response.Content.ReadFromJsonAsync<CheckoutView>())!;
        Assert.Equal("Open", checkout.Status);
        Assert.Equal(now.AddMinutes(10), checkout.ExpiresAt, TimeSpan.FromSeconds(1));
        Assert.Equal(new CheckoutItemView(reference, "Floral party dress, pink", "Gymboree", "Girls'", "Used - Excellent"), checkout.Item);
        Assert.Equal("85.00", checkout.ItemPrice);
        Assert.Equal("9.00", checkout.ShippingCost);
        Assert.Null(checkout.SalesTax);
        Assert.Null(checkout.SavedAddress);
        var detail = await _other.GetFromJsonAsync<Dictionary<string, object>>($"/api/catalog/listings/{reference}");
        Assert.Equal("Reserved", detail!["status"].ToString());
    }

    [Fact]
    public async Task Opening_checkout_again_returns_the_same_reservation()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var reference = _factory.SeedActiveListing();

        var first = await _buyer.StartedCheckoutAsync(reference);
        var again = await _buyer.StartedCheckoutAsync(reference);

        Assert.Equal(first.Id, again.Id);
    }

    [Fact]
    public async Task Another_buyer_gets_409_while_the_listing_is_reserved()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        await _other.SignInVerifiedMemberAsync(_factory, "jordan_l", "jordan@example.com");
        var reference = _factory.SeedActiveListing();
        await _buyer.StartedCheckoutAsync(reference);

        using var response = await _other.StartCheckoutAsync(reference);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("LISTING_RESERVED", (await Problem.ReadAsync(response)).Code);
    }

    [Fact]
    public async Task The_seller_cannot_check_out_their_own_listing()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var reference = _factory.SeedActiveListing(sellerId: MemberIdOf("marcus.bell@example.com"));

        using var response = await _buyer.StartCheckoutAsync(reference);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("CANNOT_BUY_OWN_LISTING", (await Problem.ReadAsync(response)).Code);
        _factory.InDatabase(db =>
        {
            Assert.Empty(db.Checkouts);
            Assert.Equal(ListingStatus.Active, db.Listings.Single().Status);
        });
    }

    [Fact]
    public async Task Visitors_get_401_and_unverified_members_403()
    {
        var reference = _factory.SeedActiveListing();

        using var visitor = await _buyer.StartCheckoutAsync(reference);
        await _other.SignInUnverifiedMemberAsync("jordan_l", "jordan@example.com");
        using var unverified = await _other.StartCheckoutAsync(reference);

        Assert.Equal(HttpStatusCode.Unauthorized, visitor.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, unverified.StatusCode);
        Assert.Equal("EMAIL_NOT_VERIFIED", (await Problem.ReadAsync(unverified)).Code);
    }

    [Fact]
    public async Task Unknown_listings_are_404_and_sold_ones_409()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var reference = _factory.SeedActiveListing();
        _factory.InDatabase(db =>
        {
            db.Listings.Single().MarkSold(DateTime.UtcNow);
            db.SaveChanges();
        });

        using var missing = await _buyer.StartCheckoutAsync("L-99999");
        using var sold = await _buyer.StartCheckoutAsync(reference);

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("LISTING_NOT_FOUND", (await Problem.ReadAsync(missing)).Code);
        Assert.Equal(HttpStatusCode.Conflict, sold.StatusCode);
        Assert.Equal("LISTING_UNAVAILABLE", (await Problem.ReadAsync(sold)).Code);
    }

    [Fact]
    public async Task Cancelling_releases_the_listing_at_once()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        await _other.SignInVerifiedMemberAsync(_factory, "jordan_l", "jordan@example.com");
        var reference = _factory.SeedActiveListing();
        var checkout = await _buyer.StartedCheckoutAsync(reference);

        using var cancel = await _buyer.DeleteAsync($"/api/checkouts/{checkout.Id}");
        using var other = await _other.StartCheckoutAsync(reference);

        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
        var released = await _buyer.GetFromJsonAsync<CheckoutView>($"/api/checkouts/{checkout.Id}");
        Assert.Equal("Released", released!.Status);
    }

    [Fact]
    public async Task Another_member_cannot_see_or_cancel_a_checkout()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        await _other.SignInVerifiedMemberAsync(_factory, "jordan_l", "jordan@example.com");
        var checkout = await _buyer.StartedCheckoutAsync(_factory.SeedActiveListing());

        using var read = await _other.GetAsync($"/api/checkouts/{checkout.Id}");
        using var cancel = await _other.DeleteAsync($"/api/checkouts/{checkout.Id}");

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, cancel.StatusCode);
    }

    [Fact]
    public async Task After_10_minutes_the_reservation_expires_and_the_listing_is_active_again()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var reference = _factory.SeedActiveListing();
        var checkout = await _buyer.StartedCheckoutAsync(reference);

        _factory.Clock.Advance(TimeSpan.FromMinutes(10));
        int expired;
        using (var scope = _factory.Services.CreateScope())
        {
            expired = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new ExpireReservationsCommand());
        }

        Assert.Equal(1, expired);
        _factory.InDatabase(db =>
        {
            var listing = db.Listings.Single();
            Assert.Equal(ListingStatus.Active, listing.Status);
            Assert.Null(listing.ReservedByCheckoutId);
            Assert.Equal(CheckoutStatus.Expired, db.Checkouts.Single().Status);
        });
        using var address = await _buyer.SetAddressAsync(checkout.Id, Checkouts.Brooklyn());
        Assert.Equal(HttpStatusCode.Conflict, address.StatusCode);
        Assert.Equal("RESERVATION_EXPIRED", (await Problem.ReadAsync(address)).Code);
    }

    [Fact]
    public async Task A_lapsed_reservation_does_not_block_the_next_buyer_before_the_job_runs()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        await _other.SignInVerifiedMemberAsync(_factory, "jordan_l", "jordan@example.com");
        var reference = _factory.SeedActiveListing();
        var first = await _buyer.StartedCheckoutAsync(reference);

        _factory.Clock.Advance(TimeSpan.FromMinutes(10));
        using var response = await _other.StartCheckoutAsync(reference);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var lapsed = await _buyer.GetFromJsonAsync<CheckoutView>($"/api/checkouts/{first.Id}");
        Assert.Equal("Expired", lapsed!.Status);
    }

    [Fact]
    public async Task A_us_address_prices_tax_on_item_plus_shipping()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var checkout = await _buyer.StartedCheckoutAsync(_factory.SeedActiveListing());

        using var response = await _buyer.SetAddressAsync(checkout.Id, Checkouts.Brooklyn());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var priced = (await response.Content.ReadFromJsonAsync<CheckoutView>())!;
        Assert.Equal("8.34", priced.SalesTax);
        Assert.Equal("102.34", priced.Total);
        Assert.Equal(new AddressView("Marcus Bell", "214 Kent Ave", "Apt 3R", "Brooklyn", "NY", "11249", "US"), priced.Address);
    }

    [Fact]
    public async Task An_address_outside_the_united_states_is_refused()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var checkout = await _buyer.StartedCheckoutAsync(_factory.SeedActiveListing());

        using var response = await _buyer.SetAddressAsync(checkout.Id, Checkouts.Brooklyn("88 Queen St W", "Toronto", null, "M5H 2M9", "CA"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("NON_US_ADDRESS", problem.Code);
        Assert.Equal("TuTuMuch ships within the United States only", problem.Title);
    }

    [Fact]
    public async Task Every_missing_address_field_is_identified()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var checkout = await _buyer.StartedCheckoutAsync(_factory.SeedActiveListing());

        using var response = await _buyer.SetAddressAsync(checkout.Id, Checkouts.Brooklyn(street: " ", city: null, state: null, zip: "1124"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("ADDRESS_INVALID", problem.Code);
        Assert.Equal(["city", "line1", "state", "zip"], problem.Errors!.Keys.Order());
        Assert.Equal("Enter a street address", problem.Errors["line1"].Message);
        Assert.Equal("Enter a 5-digit ZIP code", problem.Errors["zip"].Message);
    }

    [Fact]
    public async Task Fifty_buyers_at_once_get_one_reservation_and_49_conflicts()
    {
        using var factory = new TuTuMuchApiFactory(concurrent: true);
        factory.EnsureDatabase();
        var reference = factory.SeedActiveListing();
        var buyers = new List<HttpClient>();
        factory.InDatabase(db =>
        {
            for (var i = 0; i < 50; i++)
            {
                var member = Member.Register($"buyer_{i}", $"buyer{i}@example.com", "hash", DateTime.UtcNow);
                member.MarkEmailVerified(DateTime.UtcNow);
                db.Members.Add(member);
            }

            db.SaveChanges();
        });
        foreach (var member in MembersOf(factory))
        {
            buyers.Add(factory.CreateMemberClient().WithSession(factory, member));
        }

        var responses = await Task.WhenAll(buyers.Select(b => b.StartCheckoutAsync(reference)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(49, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        factory.InDatabase(db => Assert.Single(db.Checkouts));
        foreach (var response in responses)
        {
            response.Dispose();
        }

        foreach (var buyer in buyers)
        {
            buyer.Dispose();
        }
    }

    private static List<Member> MembersOf(TuTuMuchApiFactory factory)
    {
        var members = new List<Member>();
        factory.InDatabase(db => members.AddRange(db.Members.Where(m => m.DisplayName.StartsWith("buyer_"))));
        return members;
    }

    private Guid MemberIdOf(string email)
    {
        var id = Guid.Empty;
        _factory.InDatabase(db => id = db.Members.Single(m => m.NormalizedEmail == Member.Normalize(email)).Id);
        return id;
    }

    public void Dispose()
    {
        _buyer.Dispose();
        _other.Dispose();
        _factory.Dispose();
    }
}
