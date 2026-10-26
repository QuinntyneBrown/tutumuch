using TutuMuch.Domain.Listings;

namespace TutuMuch.Domain.Orders;

/// <summary>A shipping address as the buyer enters it. Tutu Much ships within the United States only (L2-027).</summary>
public sealed record ShippingAddress(
    string FullName,
    string Line1,
    string? Line2,
    string City,
    string State,
    string Zip,
    string Country)
{
    public const string UnitedStates = "US";
    public const int MaxName = 100;
    public const int MaxLine = 100;
    public const int MaxCity = 60;

    /// <summary>The two-letter codes of the 50 states and the District of Columbia.</summary>
    public static readonly IReadOnlySet<string> StateCodes = new HashSet<string>(StringComparer.Ordinal)
    {
        "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "DC", "FL", "GA", "HI", "ID", "IL", "IN", "IA", "KS",
        "KY", "LA", "ME", "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ", "NM", "NY", "NC",
        "ND", "OH", "OK", "OR", "PA", "RI", "SC", "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY",
    };

    /// <summary>True when the country is not the United States; checked before the field rules (L2-027 AC2).</summary>
    public static bool IsOutsideUnitedStates(string? country) =>
        !string.Equals(Clean(country) ?? UnitedStates, UnitedStates, StringComparison.OrdinalIgnoreCase);

    /// <summary>Every missing or malformed field of a US address (L2-027 AC3).</summary>
    public static IReadOnlyList<RuleViolation> Check(string? fullName, string? line1, string? line2, string? city, string? state, string? zip)
    {
        var violations = new List<RuleViolation>();
        if (Clean(fullName) is not { Length: <= MaxName })
        {
            violations.Add(new("fullName", "FULL_NAME_REQUIRED", "Enter the full name"));
        }

        if (Clean(line1) is not { Length: <= MaxLine })
        {
            violations.Add(new("line1", "STREET_REQUIRED", "Enter a street address"));
        }

        if (Clean(line2)?.Length > MaxLine)
        {
            violations.Add(new("line2", "LINE2_LENGTH", $"Keep the apartment or suite to {MaxLine} characters"));
        }

        if (Clean(city) is not { Length: <= MaxCity })
        {
            violations.Add(new("city", "CITY_REQUIRED", "Enter a city"));
        }

        if (Clean(state) is not { } code || !StateCodes.Contains(code.ToUpperInvariant()))
        {
            violations.Add(new("state", "STATE_REQUIRED", "Choose a state"));
        }

        if (Clean(zip) is not { } z || !IsZip(z))
        {
            violations.Add(new("zip", "ZIP_INVALID", "Enter a 5-digit ZIP code"));
        }

        return violations;
    }

    /// <summary>The trimmed address. Call <see cref="Check"/> first.</summary>
    public static ShippingAddress Create(string fullName, string line1, string? line2, string city, string state, string zip) =>
        new(fullName.Trim(), line1.Trim(), Clean(line2), city.Trim(), state.Trim().ToUpperInvariant(), zip.Trim(), UnitedStates);

    private static bool IsZip(string value) =>
        value.Length is 5 or 10 && value[..5].All(char.IsAsciiDigit) &&
        (value.Length == 5 || (value[5] == '-' && value[6..].All(char.IsAsciiDigit)));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
