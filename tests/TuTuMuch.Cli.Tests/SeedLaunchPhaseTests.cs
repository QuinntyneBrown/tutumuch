// Acceptance Test
// Traces to: L2-095, L2-075
// Description: Verify seed loads the whole demo catalogue in any launch phase, because the demo
// targets Phase 3, and says so; the Phase 1 product limit never blocks it.

using TuTuMuch.Cli.Tests.Support;
using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Cli.Tests;

public sealed class SeedLaunchPhaseTests
{
    [Fact]
    public async Task Seed_in_phase_1_loads_every_demo_listing_and_names_phase_3()
    {
        using var harness = new CliHarness();
        harness.Settings["Launch:Phase"] = "1";

        var run = await harness.RunAsync(["seed", "--reset"]);

        Assert.Equal(ExitCodes.Success, run.ExitCode);
        Assert.Equal(DemoCatalogue.Listings.Count, harness.InDatabase(db => db.Listings.Count(l => l.Status == ListingStatus.Active)));
        Assert.Contains("Demo data targets launch phase 3.", run.Output);
    }
}
