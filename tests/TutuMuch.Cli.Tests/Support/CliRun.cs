using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TutuMuch.Cli.Tests.Support;

/// <summary>What one run of the tool printed and returned.</summary>
public sealed record CliRun(int ExitCode, string Output)
{
    /// <summary>Runs the tool with the given settings and no database substitute; for runs that stop before any database work.</summary>
    public static async Task<CliRun> RunAsync(
        string[] args,
        string? connectionString,
        string environment = "Production",
        IDictionary<string, string?>? settings = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var values = new Dictionary<string, string?>(settings ?? new Dictionary<string, string?>());
        if (connectionString is not null)
        {
            values["ConnectionStrings:TutuMuch"] = connectionString;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        using var output = new StringWriter();
        var exitCode = await CliApp.RunAsync(args, output, configuration, environment, configureServices);
        return new CliRun(exitCode, output.ToString());
    }
}
