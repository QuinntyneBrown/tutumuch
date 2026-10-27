using System.Text.RegularExpressions;
using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Domain.Requests;

/// <summary>The fields of an item request as a client sends them (L2-097). The size may be missing: the category sets it.</summary>
public sealed record ItemRequestDetails(string? Category, string? Size, string? Keywords, decimal? MaxPrice, string? MinimumCondition);

/// <summary>
/// One member's request for an item that is not in the closet yet (L1-023): a category, keywords,
/// an optional highest price and an optional lowest condition. The size has no public setter: it is
/// always the category's accepted size (L2-013). A request is open for <see cref="OpenDays"/> after
/// it was created or last saved (L2-098) and matches listings by the rule of ADR catalog/0005.
/// </summary>
public sealed partial class ItemRequest
{
    /// <summary>The most open requests one member may have (L2-098).</summary>
    public const int MaxOpenPerMember = 20;

    /// <summary>How long a request stays open after each save (L2-098).</summary>
    public const int OpenDays = 90;

    /// <summary>How long an expired request stays on My requests before the clean-up job deletes it (L2-098).</summary>
    public const int KeepExpiredDays = 30;

    public const int MinKeywords = 3;
    public const int MaxKeywords = 100;

    private ItemRequest()
    {
    }

    public Guid Id { get; private set; }

    public Guid MemberId { get; private set; }

    public string Category { get; private set; } = string.Empty;

    public string Size { get; private set; } = string.Empty;

    /// <summary>Trimmed, with runs of whitespace collapsed to one space.</summary>
    public string Keywords { get; private set; } = string.Empty;

    public decimal? MaxPrice { get; private set; }

    /// <summary>The lowest condition accepted: this condition or better.</summary>
    public ItemCondition? MinimumCondition { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>When the member last saved it; the expiry runs from here.</summary>
    public DateTime UpdatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    /// <summary>The lower-cased keyword words, every one of which a listing must contain to match.</summary>
    public IReadOnlyList<string> Words => Keywords.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

    public bool IsOpen(DateTime now) => ExpiresAt > now;

    /// <summary>Expired for <see cref="KeepExpiredDays"/> or more: the clean-up job deletes it.</summary>
    public bool IsDueForDeletion(DateTime now) => ExpiresAt <= now.AddDays(-KeepExpiredDays);

    /// <summary>Creates an open request. Call <see cref="Check"/> first.</summary>
    public static ItemRequest Create(Guid memberId, ItemRequestDetails details, DateTime now)
    {
        var request = new ItemRequest { Id = Guid.NewGuid(), MemberId = memberId, CreatedAt = now };
        request.Update(details, now);
        return request;
    }

    /// <summary>Replaces the fields and renews the request for <see cref="OpenDays"/> days. Call <see cref="Check"/> first.</summary>
    public void Update(ItemRequestDetails details, DateTime now)
    {
        var category = FindCategory(details.Category) ?? throw new ArgumentException("A request needs an accepted category.", nameof(details));
        Category = category.Name;
        Size = category.AcceptedSize;
        Keywords = Collapse(details.Keywords) ?? throw new ArgumentException("A request needs keywords.", nameof(details));
        MaxPrice = details.MaxPrice;
        MinimumCondition = ItemConditions.FromLabel(details.MinimumCondition);
        UpdatedAt = now;
        ExpiresAt = now.AddDays(OpenDays);
    }

    /// <summary>Every broken field rule of L2-097, in form order.</summary>
    public static IReadOnlyList<RuleViolation> Check(ItemRequestDetails details)
    {
        var violations = new List<RuleViolation>();
        var category = FindCategory(details.Category);
        if (category is null)
        {
            violations.Add(new("category", "CATEGORY_INVALID", "Choose a category"));
        }
        else if (Collapse(details.Size) is { } size && !string.Equals(size, category.AcceptedSize, StringComparison.OrdinalIgnoreCase))
        {
            violations.Add(new("size", "SIZE_NOT_ACCEPTED", $"TuTuMuch accepts only items for girls in {category.Name}"));
        }

        if (Collapse(details.Keywords) is not { Length: >= MinKeywords and <= MaxKeywords })
        {
            violations.Add(new("keywords", "REQUEST_KEYWORDS_LENGTH", "Describe the item in 3 to 100 characters"));
        }

        if (details.MaxPrice is { } price && !(price >= Listing.MinPrice && price <= Listing.MaxPrice && decimal.Round(price, 2) == price))
        {
            violations.Add(new("maxPrice", "PRICE_OUT_OF_RANGE", "Enter a price from $1.00 to $5,000.00"));
        }

        if (details.MinimumCondition is not null && ItemConditions.FromLabel(details.MinimumCondition) is null)
        {
            violations.Add(new("minimumCondition", "CONDITION_INVALID", "Choose one of the 5 conditions"));
        }

        return violations;
    }

    /// <summary>
    /// The match rule (L2-100, ADR catalog/0005): the same category, a price at or below the highest
    /// price, a condition at or better than the lowest condition, every keyword word inside the
    /// title, brand or description ignoring case, and never the member's own listing.
    /// </summary>
    public bool Matches(Listing listing)
    {
        if (listing.SellerId == MemberId || !string.Equals(listing.Category, Category, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (MaxPrice is { } max && (listing.Price is not { } price || price > max))
        {
            return false;
        }

        // The enum runs from the best condition (New with tags) to the worst (Used - Fair).
        if (MinimumCondition is { } lowest && (listing.Condition is not { } condition || condition > lowest))
        {
            return false;
        }

        var text = $"{listing.Title}\n{listing.Brand}\n{listing.Description}".ToLowerInvariant();
        return Words.All(word => text.Contains(word, StringComparison.Ordinal));
    }

    private static CategoryDefinition? FindCategory(string? category) =>
        CategoryCatalog.All.FirstOrDefault(c => string.Equals(c.Name, category?.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string? Collapse(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Whitespace().Replace(value.Trim(), " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

/// <summary>
/// A listing matched an item request when it became Active (L2-100). The pair is the key, so a
/// request and a listing are matched, and emailed about, once.
/// </summary>
public sealed class ItemRequestMatch
{
    private ItemRequestMatch()
    {
    }

    public Guid ItemRequestId { get; private set; }

    public Guid ListingId { get; private set; }

    public DateTime MatchedAt { get; private set; }

    public static ItemRequestMatch Record(Guid itemRequestId, Guid listingId, DateTime now) =>
        new() { ItemRequestId = itemRequestId, ListingId = listingId, MatchedAt = now };
}
