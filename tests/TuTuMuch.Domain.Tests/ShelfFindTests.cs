// Acceptance Test
// Traces to: L2-113, L2-114, L2-116
// Description: A shelf find needs a title, a supported category, a shelf price, a store name and
// address and a seen-on date in the last 7 days; the note and the grab fee are optional and
// bounded. It is shown until 14 days after it was seen, and can be marked gone or withdrawn.

using TuTuMuch.Domain.Finds;

namespace TuTuMuch.Domain.Tests;

public sealed class ShelfFindTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now);

    public static ShelfFindDetails Valid(decimal? grabFee = null) =>
        new("  Glitter ballet flats, pink ", "Dance", 24.99m, "Target", "88 Atlantic Ave, Brooklyn, NY", Today.AddDays(-1), "On the clearance end cap", grabFee);

    [Fact]
    public void A_valid_find_is_posted_active_with_its_reference_and_trimmed_text()
    {
        var find = ShelfFind.Post(Guid.NewGuid(), 7, Valid(), Now);

        Assert.Empty(ShelfFind.Check(Valid(), Now));
        Assert.Equal("F-7", find.Reference);
        Assert.Equal("Glitter ballet flats, pink", find.Title);
        Assert.Equal(ShelfFindStatus.Active, find.Status);
        Assert.False(find.OffersGrab);
        Assert.Equal(Today.AddDays(13), find.ExpiresOn);
    }

    [Fact]
    public void Every_broken_rule_is_reported()
    {
        var details = new ShelfFindDetails("Hat", "Adults", 0.50m, "T", "Main", Today.AddDays(1), new string('n', 501), 50.01m);

        var codes = ShelfFind.Check(details, Now).Select(v => v.Code).ToList();

        Assert.Equal(
            ["TITLE_LENGTH", "CATEGORY_NOT_SUPPORTED", "SHELF_PRICE_OUT_OF_RANGE", "STORE_NAME_LENGTH", "STORE_ADDRESS_LENGTH", "SEEN_ON_OUT_OF_RANGE", "NOTE_LENGTH", "GRAB_FEE_OUT_OF_RANGE"],
            codes);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(-7, true)]
    [InlineData(-8, false)]
    [InlineData(1, false)]
    public void The_seen_on_date_is_today_or_up_to_7_days_ago(int days, bool accepted)
    {
        var details = Valid() with { SeenOn = Today.AddDays(days) };

        Assert.Equal(accepted, ShelfFind.Check(details, Now).Count == 0);
    }

    [Theory]
    [InlineData("1.00", true)]
    [InlineData("50.00", true)]
    [InlineData("0.99", false)]
    [InlineData("8.005", false)]
    public void The_grab_fee_is_1_to_50_dollars(string fee, bool accepted)
    {
        var details = Valid(decimal.Parse(fee, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(accepted, ShelfFind.Check(details, Now).Count == 0);
    }

    [Fact]
    public void A_find_expires_after_the_14th_day_after_it_was_seen()
    {
        var find = ShelfFind.Post(Guid.NewGuid(), 1, Valid() with { SeenOn = Today.AddDays(-7) }, Now);

        Assert.True(find.IsLive(Now.AddDays(7)));
        Assert.False(find.IsLive(Now.AddDays(8)));
        Assert.True(find.IsExpired(Now.AddDays(8)));
    }

    [Fact]
    public void Gone_and_withdrawn_finds_are_not_live()
    {
        var gone = ShelfFind.Post(Guid.NewGuid(), 1, Valid(), Now);
        var withdrawn = ShelfFind.Post(Guid.NewGuid(), 2, Valid(), Now);

        gone.MarkGone(Now);
        withdrawn.Withdraw(Now);

        Assert.Equal(ShelfFindStatus.Gone, gone.Status);
        Assert.False(gone.IsLive(Now));
        Assert.Equal(ShelfFindStatus.Withdrawn, withdrawn.Status);
        Assert.Throws<InvalidOperationException>(() => withdrawn.MarkGone(Now));
    }
}
