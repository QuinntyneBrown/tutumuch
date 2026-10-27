// Unit Test
// Traces to: L2-097, L2-098, L2-100
// Description: An item request takes a category, keywords, an optional highest price and an
// optional lowest condition, with the size set by the category; it is open for 90 days after it
// was saved and renewed by every save; and it matches a listing of the same category whose price,
// condition and words fit, never one of its own member's listings.

using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Requests;

namespace TuTuMuch.Domain.Tests;

public sealed class ItemRequestTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Member = Guid.NewGuid();
    private static readonly Guid Seller = Guid.NewGuid();

    private static ItemRequestDetails Details(
        string? category = "Dance", string? keywords = "ballet slippers", decimal? maxPrice = null, string? condition = null, string? size = null) =>
        new(category, size, keywords, maxPrice, condition);

    [Fact]
    public void A_complete_request_has_no_violations_and_takes_the_size_of_its_category()
    {
        Assert.Empty(ItemRequest.Check(Details(maxPrice: 20m, condition: "Used - Good")));

        var request = ItemRequest.Create(Member, Details(category: "dance", keywords: "  ballet \t  slippers "), Now);

        Assert.Equal("Dance", request.Category);
        Assert.Equal("Girls'", request.Size);
        Assert.Equal("ballet slippers", request.Keywords);
        Assert.Equal(["ballet", "slippers"], request.Words);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Furniture")]
    public void A_missing_or_unknown_category_is_refused(string? category)
    {
        var violation = Assert.Single(ItemRequest.Check(Details(category: category)));

        Assert.Equal(new RuleViolation("category", "CATEGORY_INVALID", "Choose a category"), violation);
    }

    [Fact]
    public void Any_size_but_the_category_size_is_refused()
    {
        var violation = Assert.Single(ItemRequest.Check(Details(category: "Clothing", size: "Boys' 8")));

        Assert.Equal("SIZE_NOT_ACCEPTED", violation.Code);
        Assert.Empty(ItemRequest.Check(Details(category: "Clothing", size: "girls'")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("  a   ")]
    public void Keywords_shorter_than_3_characters_are_refused(string? keywords)
    {
        var violation = Assert.Single(ItemRequest.Check(Details(keywords: keywords)));

        Assert.Equal(new RuleViolation("keywords", "REQUEST_KEYWORDS_LENGTH", "Describe the item in 3 to 100 characters"), violation);
    }

    [Fact]
    public void Keywords_of_3_and_100_characters_are_accepted_and_101_refused()
    {
        Assert.Empty(ItemRequest.Check(Details(keywords: "tut")));
        Assert.Empty(ItemRequest.Check(Details(keywords: new string('a', 100))));
        Assert.Equal("REQUEST_KEYWORDS_LENGTH", Assert.Single(ItemRequest.Check(Details(keywords: new string('a', 101)))).Code);
    }

    [Theory]
    [InlineData("0.99")]
    [InlineData("5000.01")]
    [InlineData("10.005")]
    public void A_highest_price_outside_the_listing_price_range_is_refused(string price)
    {
        var violation = Assert.Single(ItemRequest.Check(Details(maxPrice: decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture))));

        Assert.Equal(new RuleViolation("maxPrice", "PRICE_OUT_OF_RANGE", "Enter a price from $1.00 to $5,000.00"), violation);
    }

    [Fact]
    public void Highest_prices_of_1_and_5000_dollars_are_accepted()
    {
        Assert.Empty(ItemRequest.Check(Details(maxPrice: 1.00m)));
        Assert.Empty(ItemRequest.Check(Details(maxPrice: 5000.00m)));
    }

    [Fact]
    public void A_lowest_condition_outside_the_five_is_refused()
    {
        var violation = Assert.Single(ItemRequest.Check(Details(condition: "Like new")));

        Assert.Equal(new RuleViolation("minimumCondition", "CONDITION_INVALID", "Choose one of the 5 conditions"), violation);
    }

    [Fact]
    public void Every_failing_field_is_reported_together()
    {
        var fields = ItemRequest.Check(Details(category: null, keywords: "x", maxPrice: 0.5m)).Select(v => v.Field);

        Assert.Equal(["category", "keywords", "maxPrice"], fields);
    }

    [Fact]
    public void A_request_is_open_for_90_days_and_every_save_renews_it()
    {
        var request = ItemRequest.Create(Member, Details(), Now);

        Assert.Equal(new DateTime(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc), request.ExpiresAt);
        Assert.True(request.IsOpen(Now.AddDays(90).AddSeconds(-1)));
        Assert.False(request.IsOpen(Now.AddDays(90)));

        request.Update(Details(keywords: "pink ballet slippers"), Now.AddDays(100));

        Assert.True(request.IsOpen(Now.AddDays(100)));
        Assert.Equal(Now.AddDays(190), request.ExpiresAt);
        Assert.Equal(Now.AddDays(100), request.UpdatedAt);
        Assert.Equal(Now, request.CreatedAt);
        Assert.Equal("pink ballet slippers", request.Keywords);
    }

    [Fact]
    public void A_request_expired_30_days_ago_is_due_for_deletion()
    {
        var request = ItemRequest.Create(Member, Details(), Now);

        Assert.False(request.IsDueForDeletion(Now.AddDays(119)));
        Assert.True(request.IsDueForDeletion(Now.AddDays(120)));
    }

    [Fact]
    public void A_listing_of_the_same_category_with_every_word_matches()
    {
        var request = ItemRequest.Create(Member, Details(keywords: "BALLET Slippers"), Now);

        Assert.True(request.Matches(Active(title: "Pink ballet slippers")));
        Assert.True(request.Matches(Active(title: "Leather flats", brand: "Capezio", description: "Soft ballet slippers, worn to two classes.")));
        Assert.False(request.Matches(Active(title: "Ballet leotard")));
        Assert.False(request.Matches(Active(category: "Shoes", subcategory: "Dress shoes", title: "Ballet slippers")));
    }

    [Fact]
    public void A_highest_price_includes_the_price_itself_and_ignores_shipping()
    {
        var request = ItemRequest.Create(Member, Details(maxPrice: 20m), Now);

        Assert.True(request.Matches(Active(title: "Ballet slippers", price: 20.00m, shipping: 9m)));
        Assert.False(request.Matches(Active(title: "Ballet slippers", price: 20.01m)));
    }

    [Fact]
    public void A_lowest_condition_accepts_that_condition_or_better()
    {
        var request = ItemRequest.Create(Member, Details(condition: "Used - Good"), Now);

        Assert.True(request.Matches(Active(title: "Ballet slippers", condition: "New with tags")));
        Assert.True(request.Matches(Active(title: "Ballet slippers", condition: "Used - Excellent")));
        Assert.True(request.Matches(Active(title: "Ballet slippers", condition: "Used - Good")));
        Assert.False(request.Matches(Active(title: "Ballet slippers", condition: "Used - Fair", flaws: "Scuffed toes on both.")));
    }

    [Fact]
    public void A_members_own_listing_never_matches_their_request()
    {
        var request = ItemRequest.Create(Seller, Details(), Now);

        Assert.False(request.Matches(Active(title: "Ballet slippers")));
    }

    private static Listing Active(
        string category = "Dance", string subcategory = "Dance shoes", string title = "Ballet slippers", string brand = "Bloch",
        string description = "Pink leather, split sole. Clean and ready to dance.", string condition = "Used - Excellent",
        decimal price = 14m, decimal shipping = 5m, string? flaws = null)
    {
        var listing = Listing.CreateDraft(Seller, 10001, new ListingDetails(category, subcategory, null, title, description, brand, condition, flaws, price, shipping), Now);
        Assert.Empty(listing.Publish(Now, photoCount: 1));
        return listing;
    }
}
