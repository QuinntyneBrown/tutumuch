using Microsoft.Extensions.Configuration;
using TutuMuch.Cli;

// The environment is read as the .NET host reads it; unset means Production, which keeps reset off.
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") is { Length: > 0 } name ? name : "Production";
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

return await CliApp.RunAsync(args, Console.Out, configuration, environment);
