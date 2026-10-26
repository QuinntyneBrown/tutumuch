using TutuMuch.Application.Abstractions;

namespace TutuMuch.Infrastructure.Carriers;

/// <summary>
/// Stands in for the carrier tracking service adapter until a service and its credentials are
/// chosen (ADR orders/0003). It accepts no webhook, so orders stay Shipped until then; the tests
/// replace this port with a fake.
/// </summary>
internal sealed class UnconfiguredCarrierTracking : ICarrierTracking
{
    public CarrierEvent? ReadWebhook(string body, string? signature) => null;
}
