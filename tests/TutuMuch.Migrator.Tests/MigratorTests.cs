namespace TutuMuch.Migrator.Tests;

public sealed class MigratorTests
{
    [Fact]
    public void Run_without_a_connection_string_lists_the_migrations_connects_to_nothing_and_fails()
    {
        using var output = new StringWriter();

        var exitCode = MigratorApp.Run(output, connectionString: null);

        Assert.Equal(2, exitCode);
        Assert.Contains(MigratorApp.ConnectionStringVariable, output.ToString());
        Assert.Contains("_InitialCreate", output.ToString());
    }
}
