// Unit Test
// Traces to: L2-011, L2-013
// Description: The category table is held in code and sets the only accepted size per category.

using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Domain.Tests;

public sealed class CategoryCatalogTests
{
    [Fact]
    public void Catalog_holds_exactly_6_categories_and_26_subcategories()
    {
        Assert.Equal(["Clothing", "Shoes", "Dance", "Sports", "Cameras and tech", "Toys and crafts"], CategoryCatalog.All.Select(c => c.Name));
        Assert.Equal(26, CategoryCatalog.All.Sum(c => c.Subcategories.Count));
        Assert.Equal(["Dresses", "Tops", "Bottoms", "Outerwear", "Pajamas", "Swimwear"], CategoryCatalog.Find("Clothing")!.Subcategories);
        Assert.Equal(["Soccer", "Volleyball", "Gymnastics", "Swimming", "Bikes and scooters"], CategoryCatalog.Find("Sports")!.Subcategories);
        Assert.Equal(["Cameras", "Headphones", "Tablet accessories"], CategoryCatalog.Find("Cameras and tech")!.Subcategories);
    }

    [Theory]
    [InlineData("Clothing")]
    [InlineData("Shoes")]
    [InlineData("Dance")]
    [InlineData("Sports")]
    [InlineData("Cameras and tech")]
    [InlineData("Toys and crafts")]
    public void Every_category_accepts_only_girls(string category)
    {
        Assert.Equal("Girls'", CategoryCatalog.AcceptedSizeFor(category));
        Assert.Equal("Girls", CategoryCatalog.Find(category)!.SizeLabel);
    }

    [Theory]
    [InlineData("Accessories", null)]
    [InlineData("Dresses", null)]
    [InlineData("cameras and tech", null)]
    [InlineData("Clothing", "Hoodies")]
    [InlineData("Cameras and tech", "Tutus")]
    [InlineData("Shoes", "Hats")]
    public void Unknown_category_or_subcategory_is_not_accepted(string category, string? subcategory)
    {
        Assert.False(CategoryCatalog.Accepts(category, subcategory));
    }

    [Theory]
    [InlineData("Dance", "Tutus")]
    [InlineData("Sports", "Bikes and scooters")]
    [InlineData("Cameras and tech", "Tablet accessories")]
    public void A_subcategory_of_its_own_category_is_accepted(string category, string subcategory)
    {
        Assert.True(CategoryCatalog.Accepts(category, subcategory));
    }
}
