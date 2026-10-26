using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TutuMuch.Application.Abstractions;
using TutuMuch.Domain.Orders;

namespace TutuMuch.Api.Tests.Support;

/// <summary>
/// The carrier tracking service in memory (AGENTS.md: fake the port). Webhooks are JSON signed
/// with HMAC-SHA256 under <see cref="Secret"/>; <see cref="Delivered"/> builds one for the tests.
/// </summary>
public sealed class FakeCarrierTracking : ICarrierTracking
{
    public const string Secret = "carrier_test_only";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public CarrierEvent? ReadWebhook(string body, string? signature)
    {
        if (signature is null || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Sign(body)), Encoding.ASCII.GetBytes(signature)))
        {
            return null;
        }

        var e = JsonSerializer.Deserialize<WebhookBody>(body, Json)!;
        return new CarrierEvent(e.Id, TrackingNumbers.Parse(e.Carrier)!.Value, e.TrackingNumber, e.Type == "delivered", e.OccurredAt);
    }

    public static string Sign(string body) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(body)));

    /// <summary>A delivery event for the tracking number, which is sent as the carrier stores it.</summary>
    public static string Delivered(string trackingNumber, DateTime occurredAt, string carrier = "USPS", string type = "delivered") =>
        JsonSerializer.Serialize(new WebhookBody($"trk_{Guid.NewGuid():N}", carrier, trackingNumber, type, occurredAt), Json);

    private sealed record WebhookBody(string Id, string Carrier, string TrackingNumber, string Type, DateTime OccurredAt);
}
