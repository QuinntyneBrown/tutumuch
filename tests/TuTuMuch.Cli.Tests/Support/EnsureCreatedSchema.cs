using Microsoft.EntityFrameworkCore;
using TuTuMuch.Infrastructure.Persistence;

namespace TuTuMuch.Cli.Tests.Support;

/// <summary>
/// Rebuilds the schema from the model. The migrations hold SQL Server SQL that SQLite cannot run,
/// so tests rebuild with EnsureCreated; the migration path is checked by hand against LocalDB.
/// </summary>
public sealed class EnsureCreatedSchema : IDatabaseSchema
{
    public void Rebuild(TuTuMuchDbContext db, TextWriter output)
    {
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        output.WriteLine("Schema created.");
    }
}
