using System.Text.RegularExpressions;

namespace TutuMuch.Domain.Orders;

/// <summary>The carriers a seller can ship with (mock dialogs/add-tracking).</summary>
public enum Carrier
{
    Usps,
    Ups,
    FedEx,
    Dhl,
}

/// <summary>Carrier names and tracking number formats (L2-033 AC5, ADR orders/0003).</summary>
public static partial class TrackingNumbers
{
    public static string Name(Carrier carrier) => carrier switch
    {
        Carrier.Usps => "USPS",
        Carrier.Ups => "UPS",
        Carrier.FedEx => "FedEx",
        _ => "DHL",
    };

    /// <summary>The carrier with this display name, ignoring case; null for any other.</summary>
    public static Carrier? Parse(string? name)
    {
        foreach (var carrier in Enum.GetValues<Carrier>())
        {
            if (string.Equals(Name(carrier), name?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return carrier;
            }
        }

        return null;
    }

    /// <summary>The number without spaces or hyphens, upper case, when it has the carrier's format; otherwise null.</summary>
    public static string? Normalise(Carrier carrier, string? entered)
    {
        var number = Separators().Replace(entered ?? string.Empty, string.Empty).ToUpperInvariant();
        var pattern = carrier switch
        {
            Carrier.Usps => Usps(),
            Carrier.Ups => Ups(),
            Carrier.FedEx => FedEx(),
            _ => Dhl(),
        };
        return pattern.IsMatch(number) ? number : null;
    }

    /// <summary>The carrier's public tracking page for a stored number.</summary>
    public static string Url(Carrier carrier, string number) => carrier switch
    {
        Carrier.Usps => $"https://tools.usps.com/go/TrackConfirmAction?tLabels={number}",
        Carrier.Ups => $"https://www.ups.com/track?tracknum={number}",
        Carrier.FedEx => $"https://www.fedex.com/fedextrack/?trknbr={number}",
        _ => $"https://www.dhl.com/us-en/home/tracking/tracking-express.html?tracking-id={number}",
    };

    public static string InvalidMessage(Carrier carrier) => carrier switch
    {
        Carrier.Usps => "That doesn’t look like a USPS tracking number. It’s usually 22 digits.",
        Carrier.Ups => "That doesn’t look like a UPS tracking number. It starts with 1Z and has 18 characters.",
        Carrier.FedEx => "That doesn’t look like a FedEx tracking number. It’s usually 12 or 15 digits.",
        _ => "That doesn’t look like a DHL tracking number. It’s usually 10 digits.",
    };

    [GeneratedRegex(@"[\s-]")]
    private static partial Regex Separators();

    [GeneratedRegex(@"^(\d{20,22}|[A-Z]{2}\d{9}US)$")]
    private static partial Regex Usps();

    [GeneratedRegex(@"^1Z[0-9A-Z]{16}$")]
    private static partial Regex Ups();

    [GeneratedRegex(@"^(\d{12}|\d{15}|\d{20}|\d{22})$")]
    private static partial Regex FedEx();

    [GeneratedRegex(@"^\d{10,11}$")]
    private static partial Regex Dhl();
}
