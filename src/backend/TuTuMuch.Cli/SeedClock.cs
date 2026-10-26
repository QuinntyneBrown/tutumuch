namespace TuTuMuch.Cli;

/// <summary>
/// The clock every handler reads while the CLI runs. The seeder moves it back in time per step so
/// demo listings are published over the past 30 days and never trip the daily publish limit
/// (L2-051, L2-075); otherwise it reads the system clock.
/// </summary>
public sealed class SeedClock : TimeProvider
{
    private DateTimeOffset? _at;

    public override DateTimeOffset GetUtcNow() => _at ?? System.GetUtcNow();

    public void Set(DateTimeOffset at) => _at = at;

    public void Release() => _at = null;
}
