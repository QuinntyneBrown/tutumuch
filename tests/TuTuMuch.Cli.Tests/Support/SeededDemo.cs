namespace TuTuMuch.Cli.Tests.Support;

/// <summary>One `seed --reset` shared by the read-only tests of a class, since a seed runs the full photo pipeline.</summary>
public sealed class SeededDemo : IAsyncLifetime, IDisposable
{
    public CliHarness Harness { get; } = new();

    public CliRun Run { get; private set; } = new(-1, string.Empty);

    public async Task InitializeAsync() => Run = await Harness.RunAsync(["seed", "--reset"]);

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => Harness.Dispose();
}
