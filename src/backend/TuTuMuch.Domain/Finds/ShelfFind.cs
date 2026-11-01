using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Domain.Finds;

/// <summary>Where a shelf find stands (L2-116). Expiry is computed from the day it was seen, not stored.</summary>
public enum ShelfFindStatus
{
    Active,
    Gone,
    Withdrawn,
}

/// <summary>The fields of a find as the poster sends them; any may be missing (L2-113).</summary>
public sealed record ShelfFindDetails(
    string? Title,
    string? Category,
    decimal? ShelfPrice,
    string? StoreName,
    string? StoreAddress,
    DateOnly? SeenOn,
    string? Note,
    decimal? GrabFee);

/// <summary>
/// An item for girls a member saw in a store, with the store, its address, the shelf price and
/// the day it was seen, so anyone can go and buy it there (L1-025). It is not a listing: no
/// catalog query, checkout or item request sees it (ADR catalog/0009). An administrator's find
/// may offer "Grab it for me" for a fee (ADR orders/0009).
/// </summary>
public sealed class ShelfFind
{
    public const int MinTitle = 5;
    public const int MaxTitle = 80;
    public const decimal MinPrice = 1.00m;
    public const decimal MaxPrice = 5000.00m;
    public const int MinStoreName = 2;
    public const int MaxStoreName = 80;
    public const int MinStoreAddress = 5;
    public const int MaxStoreAddress = 200;
    public const int MaxNote = 500;
    public const decimal MinGrabFee = 1.00m;
    public const decimal MaxGrabFee = 50.00m;

    /// <summary>How old the seen-on date may be when the find is posted.</summary>
    public const int MaxSeenDaysAgo = 7;

    /// <summary>A find is shown until this many days after the day it was seen.</summary>
    public const int ShownForDays = 14;

    /// <summary>New finds a member may post per UTC day (L2-113, L2-051).</summary>
    public const int PostsPerDay = 20;

    private ShelfFind()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Sequential public number; the reference is "F-" followed by it.</summary>
    public long Number { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public Guid PosterId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public decimal ShelfPrice { get; private set; }

    public string StoreName { get; private set; } = string.Empty;

    public string StoreAddress { get; private set; } = string.Empty;

    public DateOnly SeenOn { get; private set; }

    public string? Note { get; private set; }

    /// <summary>The fee for "Grab it for me"; null when the poster does not offer a grab.</summary>
    public decimal? GrabFee { get; private set; }

    public ShelfFindStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>The last day the find is shown (L2-114).</summary>
    public DateOnly ExpiresOn => SeenOn.AddDays(ShownForDays);

    public bool OffersGrab => GrabFee is not null;

    public static string ReferenceFor(long number) => $"F-{number}";

    /// <summary>True once the last day it is shown has passed (UTC).</summary>
    public bool IsExpired(DateTime now) => DateOnly.FromDateTime(now) > ExpiresOn;

    /// <summary>Active and not expired: shown on Shelf finds and open to a grab.</summary>
    public bool IsLive(DateTime now) => Status == ShelfFindStatus.Active && !IsExpired(now);

    /// <summary>Every broken rule of L2-113; text is plain and only trimmed (L2-049).</summary>
    public static IReadOnlyList<RuleViolation> Check(ShelfFindDetails details, DateTime now)
    {
        var violations = new List<RuleViolation>();
        var title = Clean(details.Title);
        if (title is null || title.Length < MinTitle || title.Length > MaxTitle)
        {
            violations.Add(new("title", "TITLE_LENGTH", "Title must be 5 to 80 characters"));
        }

        if (CategoryCatalog.Find(Clean(details.Category)) is null)
        {
            violations.Add(new("category", "CATEGORY_NOT_SUPPORTED", "Choose one of TuTuMuch's categories"));
        }

        if (details.ShelfPrice is not { } price || !IsMoney(price, MinPrice, MaxPrice))
        {
            violations.Add(new("shelfPrice", "SHELF_PRICE_OUT_OF_RANGE", "Enter a price from $1.00 to $5,000.00"));
        }

        var store = Clean(details.StoreName);
        if (store is null || store.Length < MinStoreName || store.Length > MaxStoreName)
        {
            violations.Add(new("storeName", "STORE_NAME_LENGTH", "Store name must be 2 to 80 characters"));
        }

        var address = Clean(details.StoreAddress);
        if (address is null || address.Length < MinStoreAddress || address.Length > MaxStoreAddress)
        {
            violations.Add(new("storeAddress", "STORE_ADDRESS_LENGTH", "Store address must be 5 to 200 characters"));
        }

        var today = DateOnly.FromDateTime(now);
        if (details.SeenOn is not { } seen || seen > today || seen < today.AddDays(-MaxSeenDaysAgo))
        {
            violations.Add(new("seenOn", "SEEN_ON_OUT_OF_RANGE", "Choose a day in the last 7 days"));
        }

        if (Clean(details.Note)?.Length > MaxNote)
        {
            violations.Add(new("note", "NOTE_LENGTH", "Keep the note to 500 characters"));
        }

        if (details.GrabFee is { } fee && !IsMoney(fee, MinGrabFee, MaxGrabFee))
        {
            violations.Add(new("grabFee", "GRAB_FEE_OUT_OF_RANGE", "Enter a grab fee from $1.00 to $50.00"));
        }

        return violations;
    }

    /// <summary>Posts an Active find. Call <see cref="Check"/> first.</summary>
    public static ShelfFind Post(Guid posterId, long number, ShelfFindDetails details, DateTime now)
    {
        if (Check(details, now).Count > 0)
        {
            throw new ArgumentException("The find breaks the rules of L2-113.", nameof(details));
        }

        return new()
        {
            Id = Guid.NewGuid(),
            Number = number,
            Reference = ReferenceFor(number),
            PosterId = posterId,
            Title = Clean(details.Title)!,
            Category = Clean(details.Category)!,
            ShelfPrice = details.ShelfPrice!.Value,
            StoreName = Clean(details.StoreName)!,
            StoreAddress = Clean(details.StoreAddress)!,
            SeenOn = details.SeenOn!.Value,
            Note = Clean(details.Note),
            GrabFee = details.GrabFee,
            Status = ShelfFindStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>Gone from the shelf: by the poster (L2-116) or because TuTuMuch grabbed it or could not (L2-118).</summary>
    public void MarkGone(DateTime now)
    {
        if (Status == ShelfFindStatus.Withdrawn)
        {
            throw new InvalidOperationException("A withdrawn find cannot be marked gone.");
        }

        Status = ShelfFindStatus.Gone;
        UpdatedAt = now;
    }

    /// <summary>The poster takes the find down (L2-116); it answers as not found from then on.</summary>
    public void Withdraw(DateTime now)
    {
        Status = ShelfFindStatus.Withdrawn;
        UpdatedAt = now;
    }

    private static bool IsMoney(decimal value, decimal min, decimal max) =>
        value >= min && value <= max && decimal.Round(value, 2) == value;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
