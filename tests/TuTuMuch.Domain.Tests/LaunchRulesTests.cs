// Unit Test
// Traces to: L2-091, L2-092, L2-093
// Description: The launch phase decides who may sell, whether category menus show and, in Phase 1,
// how many listings the catalog may hold (fewer than the limit, every status counted).

using TuTuMuch.Domain.Launch;

namespace TuTuMuch.Domain.Tests;

public sealed class LaunchRulesTests
{
    [Theory]
    [InlineData(LaunchPhase.One, false)]
    [InlineData(LaunchPhase.Two, true)]
    [InlineData(LaunchPhase.Three, true)]
    public void Category_menus_show_from_phase_2(LaunchPhase phase, bool visible)
    {
        Assert.Equal(visible, LaunchRules.CategoryMenusVisible(phase));
    }

    [Theory]
    [InlineData(LaunchPhase.One, false, false)]
    [InlineData(LaunchPhase.One, true, true)]
    [InlineData(LaunchPhase.Two, false, false)]
    [InlineData(LaunchPhase.Two, true, true)]
    [InlineData(LaunchPhase.Three, false, true)]
    [InlineData(LaunchPhase.Three, true, true)]
    public void Only_administrators_sell_until_phase_3(LaunchPhase phase, bool isAdministrator, bool canSell)
    {
        Assert.Equal(canSell, LaunchRules.CanSell(phase, isAdministrator));
        Assert.Equal(phase == LaunchPhase.Three, LaunchRules.MemberSellingOpen(phase));
    }

    [Theory]
    [InlineData(LaunchPhase.One, 38, true)]
    [InlineData(LaunchPhase.One, 39, false)]
    [InlineData(LaunchPhase.One, 45, false)]
    [InlineData(LaunchPhase.Two, 39, true)]
    [InlineData(LaunchPhase.Three, 500, true)]
    public void A_new_listing_must_keep_phase_1_below_the_limit(LaunchPhase phase, int listings, bool allowed)
    {
        Assert.Equal(allowed, LaunchRules.CanAddListing(phase, listings, limit: 40));
    }

    [Theory]
    [InlineData(LaunchPhase.One, 39, true)]
    [InlineData(LaunchPhase.One, 40, false)]
    [InlineData(LaunchPhase.Two, 40, true)]
    [InlineData(LaunchPhase.Three, 40, true)]
    public void A_draft_publishes_in_phase_1_only_while_the_catalog_is_below_the_limit(LaunchPhase phase, int listings, bool allowed)
    {
        Assert.Equal(allowed, LaunchRules.CanPublish(phase, listings, limit: 40));
    }
}
