using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TutuMuch.Api.Auth;
using TutuMuch.Domain.Listings;
using TutuMuch.Domain.Members;

namespace TutuMuch.Api.Tests.Support;

/// <summary>Request bodies, calls and seeding for the checkout endpoints.</summary>
public static class Checkouts
{
    public static object Brooklyn(string? street = "214 Kent Ave", string? city = "Brooklyn", string? state = "NY", string? zip = "11249", string country = "US") =>
        new { fullName = "Marcus Bell", line1 = street, line2 = "Apt 3R", city, state, zip, country };

    public static Task<HttpResponseMessage> StartCheckoutAsync(this HttpClient client, string reference) =>
        client.PostAsJsonAsync("/api/checkouts", new { listingReference = reference });

    public static async Task<CheckoutView> StartedCheckoutAsync(this HttpClient client, string reference)
    {
        using var response = await client.StartCheckoutAsync(reference);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutView>())!;
    }

    public static Task<HttpResponseMessage> SetAddressAsync(this HttpClient client, Guid checkoutId, object address) =>
        client.PutAsJsonAsync($"/api/checkouts/{checkoutId}/address", address);

    public static Task<HttpResponseMessage> PayAsync(this HttpClient client, Guid checkoutId, Guid key, string total, string token = "tok_visa")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/checkouts/{checkoutId}/payments")
        {
            Content = JsonContent.Create(new { paymentMethodToken = token, expectedTotal = decimal.Parse(total, System.Globalization.CultureInfo.InvariantCulture) }),
        };
        request.Headers.Add("Idempotency-Key", key.ToString());
        return client.SendAsync(request);
    }

    /// <summary>Starts checkout, sets the Brooklyn address and submits a payment; returns the checkout and the payment.</summary>
    public static async Task<(CheckoutView Checkout, PaymentView Payment)> CheckoutAndPayAsync(this HttpClient client, string reference)
    {
        var checkout = await client.StartedCheckoutAsync(reference);
        using var address = await client.SetAddressAsync(checkout.Id, Brooklyn());
        address.EnsureSuccessStatusCode();
        var priced = (await address.Content.ReadFromJsonAsync<CheckoutView>())!;
        using var pay = await client.PayAsync(checkout.Id, Guid.NewGuid(), priced.Total!);
        pay.EnsureSuccessStatusCode();
        return (priced, (await pay.Content.ReadFromJsonAsync<PaymentView>())!);
    }

    public static Task<HttpResponseMessage> PostWebhookAsync(this HttpClient client, string body, string? signature = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhook") { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
        request.Headers.Add("Payment-Signature", signature ?? FakePaymentProvider.Sign(body));
        return client.SendAsync(request);
    }

    /// <summary>An Active $85.00 sneaker listing with $9.00 shipping by a new seller; returns its reference.</summary>
    public static string SeedActiveListing(this TutuMuchApiFactory factory, Guid? sellerId = null, long number = 30001)
    {
        var reference = Listing.ReferenceFor(number);
        factory.InDatabase(db =>
        {
            var seller = sellerId ?? SeedMember(db, $"kicks_{number}", $"kicks{number}@example.com").Id;
            var details = new ListingDetails("Shoes", "Sneakers", null, "Air Max 90, white", "Worn a handful of times. Soles clean.", "Nike", "Used - Excellent", null, 85m, 9m);
            var listing = Listing.CreateDraft(seller, number, details, DateTime.UtcNow);
            Assert.Empty(listing.Publish(sizeConfirmed: true, DateTime.UtcNow, photoCount: 1));
            db.Listings.Add(listing);
            db.SaveChanges();
        });
        return reference;
    }

    private static Member SeedMember(Infrastructure.Persistence.TutuMuchDbContext db, string name, string email)
    {
        var member = Member.Register(name, email, "hash", DateTime.UtcNow);
        member.MarkEmailVerified(DateTime.UtcNow);
        db.Members.Add(member);
        db.SaveChanges();
        return member;
    }
}

public sealed record AddressView(string FullName, string Line1, string? Line2, string City, string State, string Zip, string Country);

public sealed record CheckoutItemView(string Reference, string Title, string Brand, string Size, string Condition);

public sealed record CheckoutView(
    Guid Id,
    string Status,
    DateTime ExpiresAt,
    CheckoutItemView Item,
    string ItemPrice,
    string ShippingCost,
    string? SalesTax,
    string? Total,
    AddressView? Address,
    AddressView? SavedAddress,
    string? LastDeclineReason,
    Guid? OrderId);

public sealed record PaymentView(Guid Id, string Status);

public sealed record PurchaseView(Guid Id, string Reference, string Status, CheckoutItemView Item, string Total, string? CardBrand, string? CardLast4, string SellerDisplayName);

/// <summary>Signs a member in without the sign-in endpoint by minting their access cookie, for tests with many members.</summary>
public static class Sessions
{
    public static HttpClient WithSession(this HttpClient client, TutuMuchApiFactory factory, Member member)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AuthOptions>>().Value;
        var now = factory.Clock.GetUtcNow().UtcDateTime;
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Issuer,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, member.Id.ToString())]),
            IssuedAt = now,
            NotBefore = now,
            Expires = now + SessionCookies.AccessLifetime,
            SigningCredentials = new SigningCredentials(options.Key, SecurityAlgorithms.HmacSha256),
        });
        client.DefaultRequestHeaders.Add("Cookie", $"{SessionCookies.AccessCookie}={token}");
        return client;
    }
}
