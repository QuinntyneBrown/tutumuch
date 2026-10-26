// Acceptance Test
// Traces to: L2-075, L2-077
// Description: Verify seed keeps the daily publish limit without being blocked by it, and stops before writing when a seed photo is missing

using TutuMuch.Cli.Tests.Support;
using TutuMuch.Domain.Listings;

namespace TutuMuch.Cli.Tests;

public sealed class SeedLimitsTests
{
    [Fact]
    public async Task With_one_publish_a_day_every_listing_is_still_published()
    {
        using var harness = new CliHarness();
        harness.Settings["Selling:PublishesPerDay"] = "1";

        var run = await harness.RunAsync(["seed", "--reset"]);

        Assert.Equal(ExitCodes.Success, run.ExitCode);
        var listings = harness.InDatabase(db => db.Listings.ToList());
        Assert.Equal(DemoCatalogue.Listings.Count, listings.Count(l => l.Status == ListingStatus.Active));
        Assert.All(listings.GroupBy(l => (l.SellerId, l.PublishedAt!.Value.Date)), g => Assert.Single(g));
    }

    [Fact]
    public async Task A_missing_photo_folder_is_named_nothing_is_written_and_the_exit_code_is_4()
    {
        using var harness = new CliHarness();
        await harness.RunAsync(["reset"]);
        var missing = Path.Combine(Path.GetTempPath(), $"tutumuch-no-photos-{Guid.NewGuid():N}");
        harness.Settings["Seed:PhotosPath"] = missing;

        var run = await harness.RunAsync(["seed"]);

        Assert.Equal(ExitCodes.MissingSeedPhotos, run.ExitCode);
        Assert.Contains($"Missing seed photo: {missing}", run.Output);
        Assert.Equal(0, harness.InDatabase(db => db.Members.Count()));
    }

    [Fact]
    public async Task A_missing_photo_stops_seed_with_reset_before_the_reset()
    {
        using var harness = new CliHarness();
        Assert.Equal(ExitCodes.Success, (await harness.RunAsync(["reset"])).ExitCode);
        harness.Settings["Seed:PhotosPath"] = Path.Combine(Path.GetTempPath(), $"tutumuch-no-photos-{Guid.NewGuid():N}");

        var run = await harness.RunAsync(["seed", "--reset"]);

        Assert.Equal(ExitCodes.MissingSeedPhotos, run.ExitCode);
        Assert.DoesNotContain("Schema created.", run.Output);
    }

    [Fact]
    public void Every_catalogue_photo_is_committed_and_attributed()
    {
        var attribution = File.ReadAllText(RepositoryFiles.PathOf("src", "backend", "TutuMuch.Cli", "SeedPhotos", "ATTRIBUTION.md"));
        var committed = Directory.GetFiles(RepositoryFiles.PathOf("src", "backend", "TutuMuch.Cli", "SeedPhotos"), "*.jpg", SearchOption.AllDirectories);

        Assert.All(DemoCatalogue.PhotoFiles, f => Assert.Contains($"`{f}`", attribution));
        Assert.Equal(committed.Length, DemoCatalogue.PhotoFiles.Count);
        Assert.All(committed, f => Assert.InRange(new FileInfo(f).Length, 1, 400 * 1024));
    }
}
