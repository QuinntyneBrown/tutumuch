namespace TutuMuch.Domain.Listings;

/// <summary>One accepted category, its subcategories and the single size Tutu Much accepts for it.</summary>
public sealed record CategoryDefinition(string Name, IReadOnlyList<string> Subcategories, string AcceptedSize, string SizeLabel);

/// <summary>
/// The L2-011 table. It is code, not configuration, because the size rule is the product's
/// premise: Shoes are men's US 12 and every other category is men's L.
/// </summary>
public static class CategoryCatalog
{
    public const string MensUs12 = "Men's US 12";
    public const string MensL = "Men's L";

    public static readonly IReadOnlyList<CategoryDefinition> All =
    [
        new("Shoes", ["Sneakers", "Boots", "Dress shoes", "Sandals"], MensUs12, "US 12"),
        new("Tops", ["Shirts", "T-shirts", "Sweaters", "Hoodies"], MensL, "L"),
        new("Outerwear", ["Jackets", "Coats"], MensL, "L"),
        new("Bottoms", ["Pants", "Shorts"], MensL, "L"),
    ];

    public static CategoryDefinition? Find(string? category) =>
        All.FirstOrDefault(c => string.Equals(c.Name, category, StringComparison.Ordinal));

    /// <summary>True when the category is in the table and the subcategory, if given, belongs to it.</summary>
    public static bool Accepts(string? category, string? subcategory)
    {
        var definition = Find(category);
        return definition is not null && (subcategory is null || definition.Subcategories.Contains(subcategory));
    }

    public static string AcceptedSizeFor(string category) =>
        Find(category)?.AcceptedSize ?? throw new ArgumentException($"Category {category} is not accepted.", nameof(category));
}
