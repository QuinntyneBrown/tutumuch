// Acceptance Test
// Traces to: L2-076
// Description: Verify seed run twice adds nothing, and seed --reset always gives the same fresh demo

using TutuMuch.Cli.Tests.Support;

namespace TutuMuch.Cli.Tests;

public sealed class RepeatableSeedTests
{
    [Fact]
    public async Task Seed_is_repeatable_and_seed_with_reset_gives_the_same_fresh_demo()
    {
        using var harness = new CliHarness();
        Assert.Equal(ExitCodes.Success, (await harness.RunAsync(["seed", "--reset"])).ExitCode);
        var first = Snapshot(harness);

        var again = await harness.RunAsync(["seed"]);

        Assert.Equal(ExitCodes.Success, again.ExitCode);
        Assert.Contains("Demo data already exists. Run seed --reset for a fresh demo.", again.Output);
        Assert.Equal(first, Snapshot(harness));

        var fresh = await harness.RunAsync(["seed", "--reset"]);

        Assert.Equal(ExitCodes.Success, fresh.ExitCode);
        Assert.Equal(first, Snapshot(harness));
        Assert.Equal(DemoCatalogue.Members.Count, harness.InDatabase(db => db.Members.Count()));
        Assert.Equal(DemoCatalogue.Listings.Count, harness.InDatabase(db => db.Listings.Count()));
    }

    /// <summary>The demo as a buyer would compare it: members, and each listing's seller, fields and photo files in order.</summary>
    private static string Snapshot(CliHarness harness) => harness.InDatabase(db =>
    {
        var members = db.Members.OrderBy(m => m.Email).Select(m => $"{m.DisplayName} {m.Email} {m.EmailVerifiedAt != null}").ToList();
        var sellers = db.Members.ToDictionary(m => m.Id, m => m.DisplayName);
        var photos = db.ListingPhotos.ToList();
        var listings = db.Listings.OrderBy(l => l.Number).ToList().Select(l =>
            $"{l.Reference} {sellers[l.SellerId]} {l.Status} {l.Category}/{l.Subcategory} {l.Size} {l.Title} {l.Brand} {l.Condition} {l.Price} {l.ShippingCost} "
            + string.Join(",", photos.Where(p => p.ListingId == l.Id).OrderBy(p => p.Position).Select(p => p.OriginalHash[..12])));
        return string.Join("\n", members.Concat(listings)) + $"\nphotos {photos.Count}";
    });
}
