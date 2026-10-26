// Unit Test
// Traces to: L2-011, L2-013
// Description: The category table is held in code and sets the only accepted size per category.

using TutuMuch.Domain.Listings;

namespace TutuMuch.Domain.Tests;

public sealed class CategoryCatalogTests
{
    [Fact]
    public void Catalog_holds_exactly_4_categories_and_13_subcategories()
    {
        Assert.Equal(["Dresses", "Tops", "Outerwear", "Bottoms"], CategoryCatalog.All.Select(c => c.Name));
        Assert.Equal(13, CategoryCatalog.All.Sum(c => c.Subcategories.Count));
        Assert.Equal(["Dresses", "Skirts", "Tutus", "Rompers"], CategoryCatalog.Find("Dresses")!.Subcategories);
    }

    [Theory]
    [InlineData("Dresses", "Girls'")]
    [InlineData("Tops", "Girls'")]
    [InlineData("Outerwear", "Girls'")]
    [InlineData("Bottoms", "Girls'")]
    public void Accepted_size_follows_the_category(string category, string size)
    {
        Assert.Equal(size, CategoryCatalog.AcceptedSizeFor(category));
    }

    [Theory]
    [InlineData("Accessories", null)]
    [InlineData("Dresses", "Hoodies")]
    [InlineData("Tops", "Hats")]
    public void Unknown_category_or_subcategory_is_not_accepted(string category, string? subcategory)
    {
        Assert.False(CategoryCatalog.Accepts(category, subcategory));
    }
}
