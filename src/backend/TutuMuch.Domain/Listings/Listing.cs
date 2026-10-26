namespace TutuMuch.Domain.Listings;

/// <summary>Lifecycle states of a listing (L2-019).</summary>
public enum ListingStatus
{
    Draft,
    Active,
    Reserved,
    Sold,
    Removed,
}

/// <summary>Why a listing was taken off the catalog. A cancelled order's listing waits for the seller to relist it (L2-035).</summary>
public enum ListingRemovalReason
{
    OrderCancelled,

    /// <summary>The seller withdrew it (L2-020); a withdrawn listing cannot come back.</summary>
    Withdrawn,
}

/// <summary>The editable fields of a listing as a client sends them. Every field but the category may be missing in a draft.</summary>
public sealed record ListingDetails(
    string? Category,
    string? Subcategory,
    string? Size,
    string? Title,
    string? Description,
    string? Brand,
    string? Condition,
    string? Flaws,
    decimal? Price,
    decimal? ShippingCost)
{
    public static readonly ListingDetails Empty = new(null, null, null, null, null, null, null, null, null, null);
}

/// <summary>One broken listing rule: the request field, a stable code and the message the form shows.</summary>
public sealed record RuleViolation(string Field, string Code, string Message);

/// <summary>
/// One item offered for sale by one seller. The size has no public setter: it is always set from
/// the category (L2-013). This feature owns (new) → Draft and Draft → Active.
/// </summary>
public sealed class Listing
{
    public const int MaxTitle = 80;
    public const int MinTitle = 5;
    public const int MaxDescription = 2000;
    public const int MinDescription = 20;
    public const int MaxBrand = 50;
    public const int MaxFlaws = 500;
    public const int MinFlaws = 10;
    public const decimal MinPrice = 1.00m;
    public const decimal MaxPrice = 5000.00m;
    public const decimal MaxShipping = 100.00m;

    private Listing()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Sequential public number; the reference is "L-" followed by it.</summary>
    public long Number { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public Guid SellerId { get; private set; }

    public ListingStatus Status { get; private set; }

    public string Category { get; private set; } = string.Empty;

    public string? Subcategory { get; private set; }

    public string Size { get; private set; } = string.Empty;

    public string? Title { get; private set; }

    public string? Description { get; private set; }

    public string? Brand { get; private set; }

    public ItemCondition? Condition { get; private set; }

    public string? Flaws { get; private set; }

    public decimal? Price { get; private set; }

    public decimal? ShippingCost { get; private set; }

    public DateTime? SizeConfirmedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public DateTime? PublishedAt { get; private set; }

    /// <summary>When the seller was warned that this draft is about to be deleted (L2-019); cleared by every save.</summary>
    public DateTime? DraftReminderSentAt { get; private set; }

    /// <summary>The checkout holding a Reserved listing (L2-028); set by a conditional update so one checkout wins (L2-029).</summary>
    public Guid? ReservedByCheckoutId { get; private set; }

    public DateTime? ReservedUntil { get; private set; }

    /// <summary>
    /// Bumped by every change; the concurrency token that turns a stale edit or an edit racing a
    /// checkout into a refusal instead of a lost update (L2-020, L2-029). The API exposes it as the ETag.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>Why the listing is Removed; null in any other status.</summary>
    public ListingRemovalReason? RemovalReason { get; private set; }

    /// <summary>Whether the seller can relist it: removed because its order was cancelled (L2-035 AC4).</summary>
    public bool CanRelist => Status == ListingStatus.Removed && RemovalReason == ListingRemovalReason.OrderCancelled;

    /// <summary>Sold → Removed when its order is cancelled, so it stays off the catalog until relisted (L2-035).</summary>
    public void RemoveAfterCancellation(DateTime now)
    {
        if (Status != ListingStatus.Sold)
        {
            throw new InvalidOperationException("Only a sold listing is removed when its order is cancelled.");
        }

        Status = ListingStatus.Removed;
        RemovalReason = ListingRemovalReason.OrderCancelled;
        Touch(now);
    }

    /// <summary>Removed → Active with its original details (L2-035 AC4).</summary>
    public void Relist(DateTime now)
    {
        if (!CanRelist)
        {
            throw new InvalidOperationException("Only the listing of a cancelled order can be relisted.");
        }

        Status = ListingStatus.Active;
        RemovalReason = null;
        Touch(now);
    }

    /// <summary>
    /// Why the seller cannot change this listing now, as a stable code, or null when they can
    /// (L2-020): a Reserved or Sold listing is locked and a withdrawn one is gone for good.
    /// </summary>
    public string? EditRefusal => Status switch
    {
        ListingStatus.Reserved => "LISTING_RESERVED",
        ListingStatus.Sold => "LISTING_SOLD",
        ListingStatus.Removed when !CanRelist => "LISTING_NOT_EDITABLE",
        _ => null,
    };

    /// <summary>
    /// Replaces the fields of a published listing (Active, or Removed after a cancelled order) when
    /// the publish rules of L2-014 to L2-018 hold, reporting every failing field; otherwise leaves
    /// it unchanged. A changed category needs the size confirmed again and refreshes the confirmation.
    /// </summary>
    public IReadOnlyList<RuleViolation> Edit(ListingDetails details, bool sizeConfirmed, int photoCount, DateTime now)
    {
        if (Status == ListingStatus.Draft || EditRefusal is not null)
        {
            throw new InvalidOperationException("Only an active listing or the listing of a cancelled order can be edited.");
        }

        var violations = CheckPublish(details, sizeConfirmed, photoCount);
        if (violations.Count == 0)
        {
            var categoryChanged = details.Category != Category;
            UpdateDetails(details, now);
            if (categoryChanged)
            {
                SizeConfirmedAt = now;
            }
        }

        return violations;
    }

    /// <summary>Active → Removed, out of the catalog for good (L2-020). Withdrawing a withdrawn listing again changes nothing.</summary>
    public void Withdraw(DateTime now)
    {
        if (Status == ListingStatus.Removed && RemovalReason == ListingRemovalReason.Withdrawn)
        {
            return;
        }

        if (Status != ListingStatus.Active)
        {
            throw new InvalidOperationException("Only an active listing can be withdrawn.");
        }

        Status = ListingStatus.Removed;
        RemovalReason = ListingRemovalReason.Withdrawn;
        Touch(now);
    }

    /// <summary>A seller may not buy their own listing (L2-026 AC2).</summary>
    public bool CanBePurchasedBy(Guid buyerId) => buyerId != SellerId;

    /// <summary>Reserved → Active when the holding checkout ends without payment; any other checkout changes nothing.</summary>
    public void ReleaseReservation(Guid checkoutId, DateTime now)
    {
        if (Status == ListingStatus.Reserved && ReservedByCheckoutId == checkoutId)
        {
            Status = ListingStatus.Active;
            ReservedByCheckoutId = null;
            ReservedUntil = null;
            Touch(now);
        }
    }

    /// <summary>Reserved (or Active again after an expiry) → Sold once the payment succeeded (L2-030 AC2).</summary>
    public void MarkSold(DateTime now)
    {
        if (Status is not (ListingStatus.Reserved or ListingStatus.Active))
        {
            throw new InvalidOperationException("Only a reserved or active listing can be sold.");
        }

        Status = ListingStatus.Sold;
        ReservedByCheckoutId = null;
        ReservedUntil = null;
        Touch(now);
    }

    public static string ReferenceFor(long number) => $"L-{number}";

    /// <summary>Creates a draft. Call <see cref="CheckDraft"/> first; the category must be accepted.</summary>
    public static Listing CreateDraft(Guid sellerId, long number, ListingDetails details, DateTime now)
    {
        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            Number = number,
            Reference = ReferenceFor(number),
            SellerId = sellerId,
            Status = ListingStatus.Draft,
            CreatedAt = now,
        };
        listing.UpdateDetails(details, now);
        return listing;
    }

    /// <summary>Replaces the editable fields, trimming text and setting the size from the category.</summary>
    public void UpdateDetails(ListingDetails details, DateTime now)
    {
        Category = details.Category ?? throw new ArgumentException("A listing needs a category.", nameof(details));
        Size = CategoryCatalog.AcceptedSizeFor(Category);
        Subcategory = Clean(details.Subcategory);
        Title = Clean(details.Title);
        Description = Clean(details.Description);
        Brand = Clean(details.Brand);
        Condition = ItemConditions.FromLabel(details.Condition);
        Flaws = Condition == ItemCondition.UsedFair ? Clean(details.Flaws) : null;
        Price = details.Price;
        ShippingCost = details.ShippingCost;
        DraftReminderSentAt = null;
        Touch(now);
    }

    /// <summary>A draft not edited for this many days is deleted (L2-019).</summary>
    public const int DraftRetentionDays = 90;

    /// <summary>How many days before deletion the seller is reminded, and the least time a reminder stands before deletion.</summary>
    public const int DraftReminderDaysBefore = 7;

    public bool IsDueForDraftReminder(DateTime now) =>
        Status == ListingStatus.Draft && DraftReminderSentAt is null
        && UpdatedAt <= now.AddDays(-(DraftRetentionDays - DraftReminderDaysBefore));

    /// <summary>Old enough and reminded long enough ago: a draft that missed its reminder gets 7 more days.</summary>
    public bool IsExpiredDraft(DateTime now) =>
        Status == ListingStatus.Draft && DraftReminderSentAt is { } remindedAt
        && UpdatedAt <= now.AddDays(-DraftRetentionDays) && remindedAt <= now.AddDays(-DraftReminderDaysBefore);

    /// <summary>The day the draft is deleted when nothing changes: 90 days after the last edit, and never under 7 days after the reminder.</summary>
    public DateTime DraftDeleteOn(DateTime remindedAt)
    {
        var byAge = UpdatedAt.AddDays(DraftRetentionDays);
        var byReminder = remindedAt.AddDays(DraftReminderDaysBefore);
        return byAge > byReminder ? byAge : byReminder;
    }

    /// <summary>Records the reminder without touching UpdatedAt; the version bump makes a racing edit win.</summary>
    public void MarkDraftReminderSent(DateTime now)
    {
        if (!IsDueForDraftReminder(now))
        {
            throw new InvalidOperationException("The draft is not due for a reminder.");
        }

        DraftReminderSentAt = now;
        Version++;
    }

    /// <summary>Draft rules (L2-019): an accepted category and size, and every present field within its limits.</summary>
    public static IReadOnlyList<RuleViolation> CheckDraft(ListingDetails details)
    {
        var violations = new List<RuleViolation>();
        var category = CategoryCatalog.Find(details.Category);
        if (category is null)
        {
            violations.Add(new("category", "CATEGORY_INVALID", "Choose a category"));
        }
        else if (Clean(details.Subcategory) is { } subcategory && !category.Subcategories.Contains(subcategory))
        {
            violations.Add(new("subcategory", "CATEGORY_INVALID", "Choose a type for this category"));
        }

        if (category is not null && Clean(details.Size) is { } size && !string.Equals(size, category.AcceptedSize, StringComparison.OrdinalIgnoreCase))
        {
            violations.Add(new("size", "SIZE_NOT_ACCEPTED", $"Tutu Much accepts only {category.AcceptedSize.ToLowerInvariant()} sizes in {category.Name}"));
        }

        if (Clean(details.Title)?.Length > MaxTitle)
        {
            violations.Add(TitleLength);
        }

        if (Clean(details.Description)?.Length > MaxDescription)
        {
            violations.Add(DescriptionLength);
        }

        if (Clean(details.Brand)?.Length > MaxBrand)
        {
            violations.Add(BrandLength);
        }

        if (details.Condition is not null && ItemConditions.FromLabel(details.Condition) is null)
        {
            violations.Add(new("condition", "CONDITION_INVALID", "Choose one of the 5 conditions"));
        }

        if (Clean(details.Flaws)?.Length > MaxFlaws)
        {
            violations.Add(new("flaws", "FLAWS_LENGTH", $"Keep the flaws to {MaxFlaws} characters"));
        }

        if (details.Price is { } price && !IsMoney(price, MinPrice, MaxPrice))
        {
            violations.Add(PriceOutOfRange);
        }

        if (details.ShippingCost is { } shipping && !IsMoney(shipping, 0m, MaxShipping))
        {
            violations.Add(ShippingOutOfRange);
        }

        return violations;
    }

    /// <summary>
    /// Publishes a draft when the publish rules of L2-014 to L2-018 hold, reporting every failing
    /// field together; otherwise leaves it unchanged. <paramref name="photoCount"/> is the number
    /// of attached photos; at most 8 can be attached, so only the minimum is checked here.
    /// </summary>
    public IReadOnlyList<RuleViolation> Publish(bool sizeConfirmed, DateTime now, int photoCount)
    {
        if (Status != ListingStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft can be published.");
        }

        var details = new ListingDetails(Category, Subcategory, Size, Title, Description, Brand, Condition?.ToLabel(), Flaws, Price, ShippingCost);
        var violations = CheckPublish(details, sizeConfirmed, photoCount);
        if (violations.Count == 0)
        {
            Status = ListingStatus.Active;
            SizeConfirmedAt = now;
            PublishedAt ??= now;
            Touch(now);
        }

        return violations;
    }

    /// <summary>The publish rules (L2-014 to L2-018) on top of the draft rules, every failing field reported once.</summary>
    public static IReadOnlyList<RuleViolation> CheckPublish(ListingDetails details, bool sizeConfirmed, int photoCount)
    {
        var draft = CheckDraft(details).ToDictionary(v => v.Field);
        var violations = new List<RuleViolation>();
        void Check(string field, bool failed, RuleViolation violation)
        {
            if (draft.TryGetValue(field, out var existing))
            {
                violations.Add(existing);
            }
            else if (failed)
            {
                violations.Add(violation);
            }
        }

        var title = Clean(details.Title);
        var description = Clean(details.Description);
        var condition = ItemConditions.FromLabel(details.Condition);
        var flaws = Clean(details.Flaws);
        violations.AddRange(new[] { "category", "size" }.Where(draft.ContainsKey).Select(field => draft[field]));
        Check("photos", photoCount < 1, new("photos", "PHOTOS_REQUIRED", "Add at least one photo"));
        Check("subcategory", Clean(details.Subcategory) is null, new("subcategory", "SUBCATEGORY_REQUIRED", "Choose a type"));
        Check("title", title is null || title.Length < MinTitle, TitleLength);
        Check("description", description is null || description.Length < MinDescription, DescriptionLength);
        Check("brand", Clean(details.Brand) is null, BrandLength);
        Check("condition", details.Condition is null, new("condition", "CONDITION_REQUIRED", "Choose a condition"));
        if (condition == ItemCondition.UsedFair)
        {
            Check("flaws", flaws is null || flaws.Length < MinFlaws, new("flaws", "FLAWS_REQUIRED", "Describe the item's flaws"));
        }

        Check("price", details.Price is null, PriceOutOfRange);
        Check("shippingCost", details.ShippingCost is null, ShippingOutOfRange);
        if (!sizeConfirmed)
        {
            violations.Add(new("sizeConfirmed", "SIZE_CONFIRMATION_REQUIRED", "Confirm the label shows girls' sizing"));
        }

        return violations;
    }

    private void Touch(DateTime now)
    {
        Version++;
        UpdatedAt = now;
    }

    private static readonly RuleViolation TitleLength = new("title", "TITLE_LENGTH", "Title must be 5 to 80 characters");
    private static readonly RuleViolation DescriptionLength = new("description", "DESCRIPTION_LENGTH", "Description must be 20 to 2,000 characters");
    private static readonly RuleViolation BrandLength = new("brand", "BRAND_LENGTH", "Brand must be 1 to 50 characters");
    private static readonly RuleViolation PriceOutOfRange = new("price", "PRICE_OUT_OF_RANGE", "Enter a price from $1.00 to $5,000.00");
    private static readonly RuleViolation ShippingOutOfRange = new("shippingCost", "SHIPPING_OUT_OF_RANGE", "Enter shipping from $0.00 to $100.00");

    private static bool IsMoney(decimal value, decimal min, decimal max) =>
        value >= min && value <= max && decimal.Round(value, 2) == value;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
