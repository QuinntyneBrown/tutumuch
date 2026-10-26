using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using TuTuMuch.Application.Common;
using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Application.Catalog;

/// <summary>Order of a result set (L2-024). Newest is the default and is omitted from URLs.</summary>
public enum CatalogSort
{
    Newest,
    PriceAscending,
    PriceDescending,
}

/// <summary>
/// The raw filter values of a catalog request, exactly as the URL carries them (search-and-filter
/// URL contract): slugs for category, subcategory, condition and sort, dollar strings for prices.
/// </summary>
public sealed record CatalogFilters(
    string? Q = null,
    string? Category = null,
    IReadOnlyList<string>? Subcategories = null,
    IReadOnlyList<string>? Brands = null,
    IReadOnlyList<string>? Conditions = null,
    string? MinPrice = null,
    string? MaxPrice = null,
    string? Sort = null);

/// <summary>
/// Validated filters (L2-023, L2-024): filters combine with AND, values inside one filter with OR,
/// price bounds are inclusive. Words are the lower-cased keyword split on whitespace.
/// </summary>
public sealed partial record CatalogCriteria(
    IReadOnlyList<string> Words,
    string? Category,
    IReadOnlyList<string> Subcategories,
    IReadOnlyList<string> Brands,
    IReadOnlyList<ItemCondition> Conditions,
    decimal? MinPrice,
    decimal? MaxPrice,
    CatalogSort Sort)
{
    /// <summary>A keyword is truncated to this many characters before searching (L2-023 criterion 5).</summary>
    public const int MaxSearchTermLength = 100;

    public static readonly IReadOnlyDictionary<string, ItemCondition> ConditionSlugs = new Dictionary<string, ItemCondition>
    {
        ["new-with-tags"] = ItemCondition.NewWithTags,
        ["new-without-tags"] = ItemCondition.NewWithoutTags,
        ["used-excellent"] = ItemCondition.UsedExcellent,
        ["used-good"] = ItemCondition.UsedGood,
        ["used-fair"] = ItemCondition.UsedFair,
    };

    private static readonly IReadOnlyDictionary<string, CatalogSort> SortSlugs = new Dictionary<string, CatalogSort>
    {
        ["newest"] = CatalogSort.Newest,
        ["price-asc"] = CatalogSort.PriceAscending,
        ["price-desc"] = CatalogSort.PriceDescending,
    };

    /// <summary>"Cameras and tech" becomes "cameras-and-tech"; the URL carries categories and subcategories this way.</summary>
    public static string Slug(string name) => name.ToLowerInvariant().Replace(' ', '-');

    /// <summary>Checks every value; an unknown or malformed one is 400 CATALOG_FILTER_INVALID, min above max 400 CATALOG_PRICE_RANGE_INVALID.</summary>
    public static Result<CatalogCriteria> From(CatalogFilters filters)
    {
        CategoryDefinition? category = null;
        if (!string.IsNullOrEmpty(filters.Category))
        {
            category = CategoryCatalog.All.FirstOrDefault(c =>
                string.Equals(c.Name, filters.Category, StringComparison.OrdinalIgnoreCase) || Slug(c.Name) == filters.Category.ToLowerInvariant());
            if (category is null)
            {
                return Invalid($"Choose one of the {CategoryCatalog.All.Count} categories");
            }
        }

        var subcategories = new List<string>();
        foreach (var slug in filters.Subcategories ?? [])
        {
            var name = category?.Subcategories.FirstOrDefault(s => Slug(s) == Slug(slug));
            if (name is null)
            {
                return Invalid("Choose a subcategory of the chosen category");
            }

            subcategories.Add(name);
        }

        var conditions = new List<ItemCondition>();
        foreach (var slug in filters.Conditions ?? [])
        {
            if (!ConditionSlugs.TryGetValue(slug, out var condition))
            {
                return Invalid("Choose one of the 5 conditions");
            }

            conditions.Add(condition);
        }

        var sort = CatalogSort.Newest;
        if (!string.IsNullOrEmpty(filters.Sort) && !SortSlugs.TryGetValue(filters.Sort, out sort))
        {
            return Invalid("Choose newest, price low to high or price high to low");
        }

        if (!TryPrice(filters.MinPrice, out var min) || !TryPrice(filters.MaxPrice, out var max))
        {
            return Invalid("Enter a price of 0 or more with up to 2 decimal places");
        }

        if (min > max)
        {
            return new Error(400, "CATALOG_PRICE_RANGE_INVALID", "Minimum price must be lower than maximum");
        }

        var brands = (filters.Brands ?? []).Where(b => !string.IsNullOrWhiteSpace(b)).Select(b => b.Trim().ToLowerInvariant()).Distinct().ToList();
        return new CatalogCriteria(WordsOf(filters.Q), category?.Name, subcategories, brands, conditions, min, max, sort);
    }

    /// <summary>The visible listings these criteria keep; a keyword word must appear in title, brand or description.</summary>
    public IQueryable<Listing> Apply(IQueryable<Listing> listings)
    {
        var query = listings.Where(CatalogVisibility.IsVisible);
        foreach (var word in Words)
        {
            query = query.Where(l =>
                l.Title!.ToLower().Contains(word) || l.Brand!.ToLower().Contains(word) || l.Description!.ToLower().Contains(word));
        }

        if (Category is not null)
        {
            query = query.Where(l => l.Category == Category);
        }

        if (Subcategories.Count > 0)
        {
            query = query.Where(l => Subcategories.Contains(l.Subcategory!));
        }

        if (Brands.Count > 0)
        {
            query = query.Where(l => Brands.Contains(l.Brand!.ToLower()));
        }

        if (Conditions.Count > 0)
        {
            var conditions = Conditions.Select(c => (ItemCondition?)c).ToList();
            query = query.Where(l => conditions.Contains(l.Condition));
        }

        if (MinPrice is { } min)
        {
            query = query.Where(l => l.Price >= min);
        }

        if (MaxPrice is { } max)
        {
            query = query.Where(l => l.Price <= max);
        }

        return query;
    }

    /// <summary>
    /// With a keyword, title and brand matches come first (L2-023 criterion 3), then the sort;
    /// Id DESC is the last key so paging is stable.
    /// </summary>
    public IOrderedQueryable<Listing> Order(IQueryable<Listing> listings)
    {
        IOrderedQueryable<Listing> ordered;
        if (Words.Count > 0)
        {
            ordered = listings.OrderBy(HeadlineMiss());
            ordered = Sort switch
            {
                CatalogSort.PriceAscending => ordered.ThenBy(l => l.Price),
                CatalogSort.PriceDescending => ordered.ThenByDescending(l => l.Price),
                _ => ordered.ThenByDescending(l => l.PublishedAt),
            };
        }
        else
        {
            ordered = Sort switch
            {
                CatalogSort.PriceAscending => listings.OrderBy(l => l.Price),
                CatalogSort.PriceDescending => listings.OrderByDescending(l => l.Price),
                _ => listings.OrderByDescending(l => l.PublishedAt),
            };
        }

        return ordered.ThenByDescending(l => l.Id);
    }

    /// <summary>0 when every word is in the title or brand, 1 when some word is only in the description.</summary>
    private Expression<Func<Listing, int>> HeadlineMiss()
    {
        var listing = Expression.Parameter(typeof(Listing), "l");
        Expression? all = null;
        foreach (var word in Words)
        {
            Expression<Func<Listing, bool>> inHeadline = l => l.Title!.ToLower().Contains(word) || l.Brand!.ToLower().Contains(word);
            var body = Expression.Invoke(inHeadline, listing);
            all = all is null ? body : Expression.AndAlso(all, body);
        }

        return Expression.Lambda<Func<Listing, int>>(
            Expression.Condition(all!, Expression.Constant(0), Expression.Constant(1)), listing);
    }

    private static List<string> WordsOf(string? q)
    {
        var term = Whitespace().Replace(q ?? string.Empty, " ").Trim();
        if (term.Length > MaxSearchTermLength)
        {
            term = term[..MaxSearchTermLength];
        }

        return term.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
    }

    private static bool TryPrice(string? raw, out decimal? price)
    {
        price = null;
        if (string.IsNullOrEmpty(raw))
        {
            return true;
        }

        if (!decimal.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) || value.Scale > 2)
        {
            return false;
        }

        price = value;
        return true;
    }

    private static Error Invalid(string title) => new(400, "CATALOG_FILTER_INVALID", title);

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
