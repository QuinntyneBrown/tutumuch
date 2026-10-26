namespace TuTuMuch.Domain.Listings;

/// <summary>One accepted category, its subcategories and the single size TuTuMuch accepts for it.</summary>
public sealed record CategoryDefinition(string Name, IReadOnlyList<string> Subcategories, string AcceptedSize, string SizeLabel);

/// <summary>
/// The L2-011 table. It is code, not configuration, because the audience rule is the product's
/// premise: every category, from clothing to cameras and tech, is for girls, so every one accepts
/// only <see cref="Girls"/> (ADR general/0006).
/// </summary>
public static class CategoryCatalog
{
    public const string Girls = "Girls'";

    public static readonly IReadOnlyList<CategoryDefinition> All =
    [
        new("Clothing", ["Dresses", "Tops", "Bottoms", "Outerwear", "Pajamas", "Swimwear"], Girls, "Girls"),
        new("Shoes", ["Sneakers", "Boots", "Sandals", "Dress shoes"], Girls, "Girls"),
        new("Dance", ["Tutus", "Leotards", "Dance shoes", "Dance bags"], Girls, "Girls"),
        new("Sports", ["Soccer", "Volleyball", "Gymnastics", "Swimming", "Bikes and scooters"], Girls, "Girls"),
        new("Cameras and tech", ["Cameras", "Headphones", "Tablet accessories"], Girls, "Girls"),
        new("Toys and crafts", ["Dolls", "Arts and crafts", "Games and puzzles", "Books"], Girls, "Girls"),
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
