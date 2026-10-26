// Acceptance Test
// Traces to: L2-077
// Description: Verify the command line is parsed into a command and its flags, and anything else is a usage error

namespace TutuMuch.Cli.Tests;

public sealed class CliArgumentsTests
{
    [Theory]
    [InlineData]
    [InlineData("help")]
    [InlineData("--help")]
    [InlineData("-h")]
    public void No_command_or_a_help_flag_is_help(params string[] args)
    {
        var parsed = CliArguments.Parse(args);

        Assert.Null(parsed.Error);
        Assert.Equal(CliCommand.Help, parsed.Command);
    }

    [Fact]
    public void Reset_takes_force()
    {
        var parsed = CliArguments.Parse(["reset", "--force"]);

        Assert.Null(parsed.Error);
        Assert.Equal(CliCommand.Reset, parsed.Command);
        Assert.True(parsed.Force);
    }

    [Fact]
    public void Seed_takes_reset_and_force()
    {
        var parsed = CliArguments.Parse(["seed", "--reset", "--force"]);

        Assert.Null(parsed.Error);
        Assert.Equal(CliCommand.Seed, parsed.Command);
        Assert.True(parsed.Reset);
        Assert.True(parsed.Force);
    }

    [Fact]
    public void Seed_without_flags_neither_resets_nor_forces()
    {
        var parsed = CliArguments.Parse(["seed"]);

        Assert.False(parsed.Reset);
        Assert.False(parsed.Force);
    }

    [Theory]
    [InlineData("deploy")]
    [InlineData("reset", "--reset")]
    [InlineData("seed", "--count", "5")]
    [InlineData("help", "--force")]
    public void Anything_else_is_a_usage_error(params string[] args)
    {
        var parsed = CliArguments.Parse(args);

        Assert.NotNull(parsed.Error);
    }
}
