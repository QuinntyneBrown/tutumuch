using System.Net.Http.Json;
using TuTuMuch.Domain.Members;

namespace TuTuMuch.Api.Tests.Support;

/// <summary>Seeding and calls for the order endpoints of ship-order.</summary>
public static class Orders
{
    public const string UspsNumber = "9400 1118 9922 3456 1204 77";

    /// <summary>
    /// A Paid order bought by the signed-in <paramref name="buyer"/> through checkout and the signed
    /// payment webhook, from a new seller. Returns the order ID and a client signed in as the seller.
    /// </summary>
    public static async Task<(Guid OrderId, HttpClient Seller)> SeedPaidOrderAsync(this TuTuMuchApiFactory factory, HttpClient buyer, long listingNumber = 30001)
    {
        Member seller = null!;
        factory.InDatabase(db =>
        {
            seller = Member.Register($"kicks_{listingNumber}", $"kicks{listingNumber}@example.com", "hash", DateTime.UtcNow);
            seller.MarkEmailVerified(DateTime.UtcNow);
            db.Members.Add(seller);
            db.SaveChanges();
        });
        var (checkout, payment) = await buyer.CheckoutAndPayAsync(factory.SeedActiveListing(seller.Id, listingNumber));
        using var provider = factory.CreateMemberClient();
        (await provider.PostWebhookAsync(FakePaymentProvider.Webhook(payment.Id))).EnsureSuccessStatusCode();
        var orderId = (await buyer.GetFromJsonAsync<CheckoutView>($"/api/checkouts/{checkout.Id}"))!.OrderId!.Value;
        return (orderId, factory.CreateMemberClient().WithSession(factory, seller));
    }

    public static Task<HttpResponseMessage> AddTrackingAsync(this HttpClient client, Guid orderId, string carrier = "USPS", string trackingNumber = UspsNumber) =>
        client.PostAsJsonAsync($"/api/orders/{orderId}/shipment", new { carrier, trackingNumber });

    public static Task<HttpResponseMessage> CancelOrderAsync(this HttpClient client, Guid orderId, string? reason, string? note = null) =>
        client.PostAsJsonAsync($"/api/orders/{orderId}/cancellation", new { reason, note });

    public static Task<HttpResponseMessage> RequestCancellationAsync(this HttpClient client, Guid orderId, string? message) =>
        client.PostAsJsonAsync($"/api/orders/{orderId}/cancellation-request", new { message });

    public static Task<HttpResponseMessage> RespondToCancellationAsync(this HttpClient client, Guid orderId, string? decision) =>
        client.PostAsJsonAsync($"/api/orders/{orderId}/cancellation-request/response", new { decision });

    public static Task<HttpResponseMessage> RelistAsync(this HttpClient client, string reference) =>
        client.PostAsync($"/api/listings/{reference}/relist", null);

    public static Task<HttpResponseMessage> ConfirmReceiptAsync(this HttpClient client, Guid orderId) =>
        client.PostAsync($"/api/orders/{orderId}/receipt", null);

    /// <summary>Ships the order as the seller and has the carrier report delivery at <paramref name="deliveredAt"/>.</summary>
    public static async Task DeliverAsync(this TuTuMuchApiFactory factory, Guid orderId, HttpClient seller, DateTime deliveredAt)
    {
        (await seller.AddTrackingAsync(orderId)).EnsureSuccessStatusCode();
        using var carrier = factory.CreateMemberClient();
        (await carrier.PostCarrierWebhookAsync(FakeCarrierTracking.Delivered("9400111899223456120477", deliveredAt))).EnsureSuccessStatusCode();
    }

    public static Task<HttpResponseMessage> PostCarrierWebhookAsync(this HttpClient client, string body, string? signature = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/carriers/webhook") { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
        request.Headers.Add("Carrier-Signature", signature ?? FakeCarrierTracking.Sign(body));
        return client.SendAsync(request);
    }
}

public sealed record TimelineStepView(string Status, DateTime? At);

public sealed record TrackingView(string Carrier, string Number, string Url);

public sealed record SaleView(
    Guid Id,
    string Reference,
    string Status,
    DateTime PaidAt,
    DateTime ShipBy,
    CheckoutItemView Item,
    string ItemPrice,
    string ShippingCost,
    string SalesTax,
    string Total,
    string BuyerDisplayName,
    AddressView ShipTo,
    IReadOnlyList<TimelineStepView> Timeline,
    TrackingView? Tracking);

public sealed record OrderView(Guid Id, string Reference, string Status, DateTime ShipBy, IReadOnlyList<TimelineStepView> Timeline, TrackingView? Tracking, DateTime? AutoCompletesAt = null);

public sealed record CancellationView(string Reason, string ReasonLabel, string? Note);

public sealed record CancellationRequestView(string? Message, string Status, DateTime RequestedAt);

public sealed record CancelledSaleView(Guid Id, string Status, CancellationView? Cancellation, CancellationRequestView? CancellationRequest, bool CanRelist);

public sealed record CancelledPurchaseView(Guid Id, string Status, CancellationView? Cancellation, CancellationRequestView? CancellationRequest);
