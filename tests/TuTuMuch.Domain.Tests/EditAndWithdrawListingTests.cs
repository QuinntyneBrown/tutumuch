// Unit Test
// Traces to: L2-013, L2-014, L2-020
// Description: An Active listing is edited under the publish rules and withdrawn for good; every
// change bumps the version, and a Reserved, Sold or withdrawn listing refuses to change.

using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Domain.Tests;

public sealed class EditAndWithdrawListingTests
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    private static ListingDetails Complete(string category = "Clothing", string subcategory = "Dresses") => new(
        category, subcategory, null, "Rainbow tulle tutu, pink", "Light wear, colours still bright.", "Gymboree", "Used - Good", null, 60.00m, 8.00m);

    private static Listing Active()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), 10001, Complete(), Now);
        Assert.Empty(listing.Publish(Now, photoCount: 1));
        return listing;
    }

    [Fact]
    public void Every_change_bumps_the_version()
    {
        var listing = Active();
        var before = listing.Version;

        Assert.Empty(listing.Edit(Complete() with { Price = 40m }, photoCount: 1, Now));

        Assert.Equal(before + 1, listing.Version);
        Assert.Equal(40m, listing.Price);
    }

    [Fact]
    public void A_refused_edit_changes_nothing()
    {
        var listing = Active();
        var before = listing.Version;

        var violations = listing.Edit(Complete() with { Condition = "Used - Fair", Price = 5200m }, photoCount: 0, Now);

        Assert.Equal(["photos", "flaws", "price"], violations.Select(v => v.Field));
        Assert.Equal(before, listing.Version);
        Assert.Equal(60m, listing.Price);
    }

    [Fact]
    public void A_new_category_resets_the_size()
    {
        var listing = Active();
        var later = Now.AddHours(1);

        Assert.Empty(listing.Edit(Complete("Dance", "Leotards"), photoCount: 1, later));

        Assert.Equal("Girls'", listing.Size);
    }

    [Fact]
    public void Withdrawing_removes_the_listing_for_good()
    {
        var listing = Active();

        listing.Withdraw(Now);
        listing.Withdraw(Now);

        Assert.Equal(ListingStatus.Removed, listing.Status);
        Assert.Equal(ListingRemovalReason.Withdrawn, listing.RemovalReason);
        Assert.False(listing.CanRelist);
        Assert.Equal("LISTING_NOT_EDITABLE", listing.EditRefusal);
        Assert.Throws<InvalidOperationException>(() => listing.Edit(Complete(), 1, Now));
    }

    [Fact]
    public void A_sold_listing_is_locked()
    {
        var listing = Active();
        listing.MarkSold(Now);

        Assert.Equal("LISTING_SOLD", listing.EditRefusal);
        Assert.Throws<InvalidOperationException>(() => listing.Withdraw(Now));
    }

    [Fact]
    public void A_draft_cannot_be_withdrawn()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), 10001, Complete(), Now);

        Assert.Throws<InvalidOperationException>(() => listing.Withdraw(Now));
    }
}
