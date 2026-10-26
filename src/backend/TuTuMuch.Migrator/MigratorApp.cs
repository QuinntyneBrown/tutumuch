using Microsoft.EntityFrameworkCore;
using TuTuMuch.Infrastructure.Persistence;

namespace TuTuMuch.Migrator;

/// <summary>Applies the EF Core migrations of TuTuMuch.Infrastructure to TuTuMuch database.</summary>
public static class MigratorApp
{
    public const string ConnectionStringVariable = "ConnectionStrings__TuTuMuch";

    /// <summary>
    /// Applies pending migrations when a connection string is given. Without one it lists the
    /// migrations in this build, connects to nothing and fails, so a misconfigured deploy stops.
    /// </summary>
    public static int Run(TextWriter output, string? connectionString)
    {
        var options = new DbContextOptionsBuilder<TuTuMuchDbContext>()
            .UseSqlServer(string.IsNullOrWhiteSpace(connectionString) ? "Server=unset" : connectionString)
            .Options;
        using var db = new TuTuMuchDbContext(options);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            output.WriteLine($"Set {ConnectionStringVariable} to apply these migrations:");
            foreach (var migration in db.Database.GetMigrations())
            {
                output.WriteLine($"  {migration}");
            }

            return 2;
        }

        ApplyMigrations(db, output);
        return 0;
    }

    /// <summary>Applies every pending migration. TuTuMuch.Cli reset calls it too, so there is one migration path (ADR platform/0010).</summary>
    public static void ApplyMigrations(DbContext db, TextWriter output)
    {
        db.Database.Migrate();
        output.WriteLine("Migrations applied.");
    }
}
