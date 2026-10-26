using Microsoft.EntityFrameworkCore;
using TuTuMuch.Infrastructure.Persistence;
using TuTuMuch.Migrator;

namespace TuTuMuch.Cli;

/// <summary>Drops and rebuilds TuTuMuch database. A seam so tests can rebuild SQLite without the SQL Server migrations.</summary>
public interface IDatabaseSchema
{
    void Rebuild(TuTuMuchDbContext db, TextWriter output);
}

/// <summary>Drops the database and applies every migration through the migrator's own code (L2-072).</summary>
public sealed class MigratedSchema : IDatabaseSchema
{
    public void Rebuild(TuTuMuchDbContext db, TextWriter output)
    {
        output.WriteLine(db.Database.EnsureDeleted() ? "Database dropped." : "No database to drop.");
        MigratorApp.ApplyMigrations(db, output);
    }
}
