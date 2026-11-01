// Acceptance Test
// Traces to: L2-113, L2-114
// Description: Verify seed posts the demo shelf finds through the same command as the API, all live,
// by a demo member and without a grab offer, since only administrators offer grabs.

using TuTuMuch.Cli.Tests.Support;
using TuTuMuch.Domain.Finds;

namespace TuTuMuch.Cli.Tests;

public sealed class SeedFindsTests(SeededDemo demo) : IClassFixture<SeededDemo>
{
    [Fact]
    public void The_demo_has_three_live_shelf_finds_without_grab_offers()
    {
        var finder = DemoCatalogue.Members.Single(m => m.DisplayName == DemoCatalogue.FindingMember);
        var finds = demo.Harness.InDatabase(db =>
        {
            var posterId = db.Members.Single(m => m.Email == finder.Email).Id;
            return db.ShelfFinds.Where(f => f.PosterId == posterId).ToList();
        });

        Assert.Equal(ExitCodes.Success, demo.Run.ExitCode);
        Assert.Equal(DemoCatalogue.Finds.Count, finds.Count);
        Assert.All(finds, f => Assert.True(f.IsLive(DateTime.UtcNow)));
        Assert.All(finds, f => Assert.Null(f.GrabFee));
        Assert.Contains($"Seeded {DemoCatalogue.Finds.Count} shelf finds.", demo.Run.Output);
    }
}
