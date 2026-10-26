namespace TutuMuch.Cli;

/// <summary>The commands of TutuMuch.Cli.</summary>
public enum CliCommand
{
    Help,
    Reset,
    Seed,
}

/// <summary>
/// The parsed command line: a command and its flags, or the reason it is not a valid one
/// (L2-077). Plain parsing: two commands and two flags do not need System.CommandLine.
/// </summary>
public sealed record CliArguments(CliCommand Command, bool Reset, bool Force, string? Error)
{
    public const string Usage = """
        Usage: TutuMuch.Cli <command> [options]

        Commands:
          reset [--force]          Drop the database, apply every migration and empty the photo store.
          seed [--reset] [--force] Load the demo members and listings. --reset runs reset first.
          help                     Show this help.

        Options:
          --force                  Allow reset outside the Development environment.
          --reset                  (seed) Reset before seeding, for the same fresh demo every time.

        Settings: ConnectionStrings__TutuMuch (required), DOTNET_ENVIRONMENT, PhotoStorage__RootPath.
        For a developer's own machine only.
        """;

    private static readonly IReadOnlyDictionary<CliCommand, string[]> Flags = new Dictionary<CliCommand, string[]>
    {
        [CliCommand.Help] = [],
        [CliCommand.Reset] = ["--force"],
        [CliCommand.Seed] = ["--reset", "--force"],
    };

    public static CliArguments Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            return new(CliCommand.Help, false, false, null);
        }

        CliCommand? command = args[0] switch
        {
            "help" or "--help" or "-h" => CliCommand.Help,
            "reset" => CliCommand.Reset,
            "seed" => CliCommand.Seed,
            _ => null,
        };
        if (command is not { } known)
        {
            return Invalid($"Unknown command '{args[0]}'.");
        }

        var flags = args.Skip(1).ToList();
        if (flags.FirstOrDefault(f => !Flags[known].Contains(f, StringComparer.Ordinal)) is { } unknown)
        {
            return Invalid($"Unknown option '{unknown}' for {args[0]}.");
        }

        return new(known, flags.Contains("--reset"), flags.Contains("--force"), null);
    }

    private static CliArguments Invalid(string error) => new(CliCommand.Help, false, false, error);
}
