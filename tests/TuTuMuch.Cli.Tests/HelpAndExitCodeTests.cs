// Acceptance Test
// Traces to: L2-077
// Description: Verify usage, the exit codes and the one-line messages of TuTuMuch.Cli

using TuTuMuch.Cli.Tests.Support;

namespace TuTuMuch.Cli.Tests;

public sealed class HelpAndExitCodeTests
{
    [Fact]
    public async Task No_arguments_prints_the_commands_and_options_and_exits_0()
    {
        var run = await CliRun.RunAsync([], connectionString: null);

        Assert.Equal(ExitCodes.Success, run.ExitCode);
        Assert.Contains("reset", run.Output);
        Assert.Contains("seed", run.Output);
        Assert.Contains("--force", run.Output);
        Assert.Contains("--reset", run.Output);
    }

    [Fact]
    public async Task An_unknown_command_prints_the_usage_and_exits_1()
    {
        var run = await CliRun.RunAsync(["deploy"], connectionString: null);

        Assert.Equal(1, run.ExitCode);
        Assert.Contains("Unknown command 'deploy'", run.Output);
        Assert.Contains("Usage:", run.Output);
    }

    [Theory]
    [InlineData("reset")]
    [InlineData("seed")]
    public async Task Without_a_connection_string_it_names_the_setting_and_exits_2(string command)
    {
        var run = await CliRun.RunAsync([command], connectionString: null, environment: "Development");

        Assert.Equal(2, run.ExitCode);
        Assert.Contains("ConnectionStrings__TuTuMuch", run.Output);
    }

    [Fact]
    public async Task An_unreachable_database_prints_the_failure_and_exits_5()
    {
        var missingFolder = Path.Combine(Path.GetTempPath(), $"tutumuch-missing-{Guid.NewGuid():N}", "demo.db");
        using var harness = new CliHarness();

        var run = await harness.RunAsync(["seed"], sqliteConnectionString: $"Data Source={missingFolder}");

        Assert.Equal(5, run.ExitCode);
        Assert.Contains("Seed failed", run.Output);
    }
}
