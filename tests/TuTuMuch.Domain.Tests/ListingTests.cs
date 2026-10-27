// Unit Test
// Traces to: L2-013, L2-014, L2-015, L2-016, L2-018, L2-019
// Description: The Listing aggregate sets its size from the category, saves drafts with only a
// category, and applies every publish rule at once.

using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Domain.Tests;

public sealed class ListingTests
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    private static ListingDetails Complete(string category = "Clothing", string subcategory = "Dresses") => new(
        category, subcategory, null, "Rainbow tulle tutu, pink", "Light wear, colours still bright.", "Gymboree", "Used - Good", null, 60.00m, 8.00m);

    [Fact]
    public void A_draft_needs_only_a_category_and_takes_its_size_from_it()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), 10001, ListingDetails.Empty with { Category = "Clothing" }, Now);

        Assert.Equal(ListingStatus.Draft, listing.Status);
        Assert.Equal("Girls'", listing.Size);
        Assert.Equal("L-10001", listing.Reference);
    }

    [Fact]
    public void Changing_the_category_changes_the_size()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), 10001, Complete("Clothing", "Tops"), Now);

        listing.UpdateDetails(Complete(), Now);

        Assert.Equal("Girls'", listing.Size);
    }

    [Fact]
    public void Text_is_trimmed()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), 10001, Complete() with { Title = "  Floral party dress  ", Brand = " Gymboree " }, Now);

        Assert.Equal("Floral party dress", listing.Title);
        Assert.Equal("Gymboree", listing.Brand);
    }

    [Fact]
    public void Draft_rules_reject_a_size_other_than_the_accepted_one()
    {
        var violations = Listing.CheckDraft(Complete() with { Size = "11" });

        Assert.Equal("SIZE_NOT_ACCEPTED", Assert.Single(violations).Code);
    }

    [Theory]
    [InlineData("Girls'")]
    [InlineData("girls'")]
    public void Draft_rules_accept_the_accepted_size_sent_by_a_client(string size)
    {
        Assert.Empty(Listing.CheckDraft(Complete() with { Size = size }));
    }

    [Fact]
    public void Publishing_a_complete_listing_makes_it_active()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), 10001, Complete(), Now);

        var violations = listing.Publish(Now, photoCount: 1);

        Assert.Empty(violations);
        Assert.Equal(ListingStatus.Active, listing.Status);
        Assert.Equal(Now, listing.PublishedAt);
    }

    [Fact]
    public void Publishing_without_photos_is_refused_with_add_at_least_one_photo()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), 10001, Complete(), Now);

        var violation = Assert.Single(listing.Publish(Now, photoCount: 0));

        Assert.Equal(("photos", "PHOTOS_REQUIRED", "Add at least one photo"), (violation.Field, violation.Code, violation.Message));
        Assert.Equal(ListingStatus.Draft, listing.Status);
    }

    [Fact]
    public void Publishing_an_empty_draft_reports_every_missing_field()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), 10001, ListingDetails.Empty with { Category = "Clothing" }, Now);

        var violations = listing.Publish(Now, photoCount: 0);

        Assert.Equal(
            ["photos", "subcategory", "title", "description", "brand", "condition", "price", "shippingCost"],
            violations.Select(v => v.Field));
    }

    [Theory]
    [InlineData("Tutu", "TITLE_LENGTH")]
    [InlineData("This title is far too long for a listing on TuTuMuch because it goes on and on and on.", "TITLE_LENGTH")]
    public void Title_must_be_5_to_80_characters(string title, string code)
    {
        Assert.Equal(code, PublishViolation(Complete() with { Title = title }));
    }

    [Fact]
    public void Description_must_be_20_to_2000_characters()
    {
        Assert.Equal("DESCRIPTION_LENGTH", PublishViolation(Complete() with { Description = new string('a', 19) }));
        Assert.Equal("DESCRIPTION_LENGTH", PublishViolation(Complete() with { Description = new string('a', 2001) }));
        Assert.Null(PublishViolation(Complete() with { Description = new string('a', 20) }));
    }

    [Theory]
    [InlineData("0.99")]
    [InlineData("5000.01")]
    [InlineData("10.005")]
    public void Price_outside_1_to_5000_or_with_more_than_2_decimals_is_refused(string price)
    {
        Assert.Equal("PRICE_OUT_OF_RANGE", PublishViolation(Complete() with { Price = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture) }));
    }

    [Theory]
    [InlineData("1.00")]
    [InlineData("5000.00")]
    public void Price_at_the_limits_is_accepted(string price)
    {
        Assert.Null(PublishViolation(Complete() with { Price = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture) }));
    }

    [Theory]
    [InlineData("100.01")]
    [InlineData("-0.01")]
    public void Shipping_outside_0_to_100_is_refused(string shipping)
    {
        Assert.Equal("SHIPPING_OUT_OF_RANGE", PublishViolation(Complete() with { ShippingCost = decimal.Parse(shipping, System.Globalization.CultureInfo.InvariantCulture) }));
    }

    [Fact]
    public void Used_fair_needs_flaws_of_at_least_10_characters()
    {
        var fair = Complete() with { Condition = "Used - Fair" };

        Assert.Equal("FLAWS_REQUIRED", PublishViolation(fair));
        Assert.Equal("FLAWS_REQUIRED", PublishViolation(fair with { Flaws = "Scuffed." }));
        Assert.Null(PublishViolation(fair with { Flaws = "Scuff on the left toe." }));
    }

    [Fact]
    public void Condition_outside_the_five_values_is_refused_even_in_a_draft()
    {
        Assert.Equal("CONDITION_INVALID", Assert.Single(Listing.CheckDraft(Complete() with { Condition = "Like new" })).Code);
    }

    [Fact]
    public void Only_a_draft_can_be_published()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), 10001, Complete(), Now);
        listing.Publish(Now, photoCount: 1);

        Assert.Throws<InvalidOperationException>(() => listing.Publish(Now, photoCount: 1));
    }

    private static string? PublishViolation(ListingDetails details)
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), 10001, details, Now);
        return listing.Publish(Now, photoCount: 1).SingleOrDefault()?.Code;
    }
}
