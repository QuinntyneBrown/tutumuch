namespace TuTuMuch.Cli.Tests.Support;

/// <summary>Finds files of the repository from the test output folder.</summary>
public static class RepositoryFiles
{
    public static string PathOf(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return Path.Combine([directory?.FullName ?? throw new InvalidOperationException("The repository root was not found."), .. parts]);
    }
}
