// Acceptance Test
// Traces to: L2-072, L2-073
// Description: Verify reset rebuilds an empty database, empties the photo store, and only runs in Development or with --force

using Microsoft.EntityFrameworkCore;
using TutuMuch.Cli.Tests.Support;
using TutuMuch.Domain.Members;

namespace TutuMuch.Cli.Tests;

public sealed class ResetTests : IDisposable
{
    private readonly CliHarness _harness = new();
    private readonly string _photoRoot = Path.Combine(Path.GetTempPath(), $"tutumuch-cli-tests-{Guid.NewGuid():N}");

    public ResetTests()
    {
        _harness.Settings["PhotoStorage:RootPath"] = _photoRoot;
    }

    [Fact]
    public async Task Reset_in_development_leaves_an_empty_schema_and_an_empty_photo_store()
    {
        await GivenAMemberAndStoredPhotosAsync();

        var run = await _harness.RunAsync(["reset"], environment: "Development");

        Assert.Equal(ExitCodes.Success, run.ExitCode);
        Assert.Equal(0, _harness.InDatabase(db => db.Members.Count()));
        Assert.Equal(0, _harness.InDatabase(db => db.Listings.Count()));
        Assert.False(Directory.Exists(Path.Combine(_photoRoot, "public")));
        Assert.False(Directory.Exists(Path.Combine(_photoRoot, "private")));
    }

    [Fact]
    public async Task Reset_creates_a_missing_database()
    {
        var run = await _harness.RunAsync(["reset"], environment: "Development");

        Assert.Equal(ExitCodes.Success, run.ExitCode);
        Assert.Equal(0, _harness.InDatabase(db => db.Members.Count()));
    }

    [Fact]
    public async Task Reset_lists_the_steps_it_took()
    {
        var run = await _harness.RunAsync(["reset"], environment: "Development");

        Assert.Contains("Schema created.", run.Output);
        Assert.Contains("Photo store emptied", run.Output);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task Reset_outside_development_without_force_changes_nothing_and_exits_3(string environment)
    {
        await GivenAMemberAndStoredPhotosAsync();

        var run = await _harness.RunAsync(["reset"], environment: environment);

        Assert.Equal(ExitCodes.ResetRefused, run.ExitCode);
        Assert.Contains("Reset only runs in Development or with --force", run.Output);
        Assert.Equal(1, _harness.InDatabase(db => db.Members.Count()));
        Assert.True(File.Exists(Path.Combine(_photoRoot, "public", "ab", "photo.webp")));
    }

    [Fact]
    public async Task Seed_with_reset_is_guarded_too()
    {
        await GivenAMemberAndStoredPhotosAsync();

        var run = await _harness.RunAsync(["seed", "--reset"], environment: "Production");

        Assert.Equal(ExitCodes.ResetRefused, run.ExitCode);
        Assert.Equal(1, _harness.InDatabase(db => db.Members.Count()));
    }

    [Fact]
    public async Task Reset_with_force_runs_in_any_environment()
    {
        await GivenAMemberAndStoredPhotosAsync();

        var run = await _harness.RunAsync(["reset", "--force"], environment: "Staging");

        Assert.Equal(ExitCodes.Success, run.ExitCode);
        Assert.Equal(0, _harness.InDatabase(db => db.Members.Count()));
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Reset_prints_the_server_and_database_but_never_the_credentials(string environment)
    {
        var run = await _harness.RunAsync(["reset"], environment: environment);

        Assert.Contains("Target: server (localdb)\\MSSQLLocalDB, database TutuMuchDemo", run.Output);
        Assert.DoesNotContain("not-a-real-secret", run.Output);
        Assert.DoesNotContain("demo;", run.Output);
    }

    public void Dispose()
    {
        _harness.Dispose();
        if (Directory.Exists(_photoRoot))
        {
            Directory.Delete(_photoRoot, recursive: true);
        }
    }

    private async Task GivenAMemberAndStoredPhotosAsync()
    {
        Assert.Equal(ExitCodes.Success, (await _harness.RunAsync(["reset"])).ExitCode);
        _harness.InDatabase(db =>
        {
            db.Members.Add(Member.Register("ExistingMember", "existing@example.test", "hash", DateTime.UtcNow));
            return db.SaveChanges();
        });
        foreach (var file in new[] { Path.Combine("public", "ab", "photo.webp"), Path.Combine("private", "originals", "photo") })
        {
            var path = Path.Combine(_photoRoot, file);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, [1, 2, 3]);
        }
    }
}
