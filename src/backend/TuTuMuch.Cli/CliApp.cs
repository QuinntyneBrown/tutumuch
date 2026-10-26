using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TuTuMuch.Application;
using TuTuMuch.Infrastructure;

namespace TuTuMuch.Cli;

/// <summary>
/// Runs one command of TuTuMuch.Cli, the developer tool that resets a local install and seeds demo
/// data (ADR platform/0010). It composes the same Application and Infrastructure services as the
/// API; <paramref name="configureServices"/> lets tests swap the database and photo store.
/// </summary>
public static class CliApp
{
    public const string ConnectionStringVariable = "ConnectionStrings__TuTuMuch";

    public static async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        IConfiguration configuration,
        string environmentName,
        Action<IServiceCollection>? configureServices = null)
    {
        var arguments = CliArguments.Parse(args);
        if (arguments.Error is { } error)
        {
            output.WriteLine(error);
            output.WriteLine(CliArguments.Usage);
            return ExitCodes.Usage;
        }

        if (arguments.Command == CliCommand.Help)
        {
            output.WriteLine(CliArguments.Usage);
            return ExitCodes.Success;
        }

        var connectionString = configuration.GetConnectionString("TuTuMuch");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            output.WriteLine($"Set {ConnectionStringVariable} to the database to {arguments.Command.ToString().ToLowerInvariant()}.");
            return ExitCodes.NoConnectionString;
        }

        await using var services = BuildServices(configuration, configureServices);
        await using var scope = services.CreateAsyncScope();
        var step = arguments.Command == CliCommand.Reset ? "Reset" : "Seed";
        try
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DemoSeeder>();
            if (arguments.Command == CliCommand.Seed && seeder.FindMissingPhoto() is { } missing)
            {
                output.WriteLine($"Missing seed photo: {missing}");
                return ExitCodes.MissingSeedPhotos;
            }

            if (arguments.Command == CliCommand.Reset || arguments.Reset)
            {
                output.WriteLine(ResetGuard.DescribeTarget(connectionString));
                if (!ResetGuard.Allows(environmentName, arguments.Force))
                {
                    output.WriteLine($"Reset only runs in Development or with --force; the environment is {environmentName}. Nothing was changed.");
                    return ExitCodes.ResetRefused;
                }

                step = "Reset";
                scope.ServiceProvider.GetRequiredService<DemoReset>().Run(output);
            }

            if (arguments.Command == CliCommand.Seed)
            {
                step = "Seed";
                await seeder.SeedAsync(output, CancellationToken.None);
            }

            return ExitCodes.Success;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            output.WriteLine($"{step} failed: {exception.GetBaseException().Message}");
            return ExitCodes.Failed;
        }
    }

    /// <summary>The services one run uses; tests build the same set to read what a run wrote.</summary>
    public static ServiceProvider BuildServices(IConfiguration configuration, Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.RemoveAll<TimeProvider>();
        services.AddSingleton<SeedClock>();
        services.AddSingleton<TimeProvider>(sp => sp.GetRequiredService<SeedClock>());
        services.AddSingleton<IDatabaseSchema, MigratedSchema>();
        services.AddScoped<DemoReset>();
        services.AddScoped<DemoSeeder>();
        configureServices?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
