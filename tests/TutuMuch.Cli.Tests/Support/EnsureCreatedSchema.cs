using Microsoft.EntityFrameworkCore;
using TutuMuch.Infrastructure.Persistence;

namespace TutuMuch.Cli.Tests.Support;

/// <summary>
/// Rebuilds the schema from the model. The migrations hold SQL Server SQL that SQLite cannot run,
/// so tests rebuild with EnsureCreated; the migration path is checked by hand against LocalDB.
/// </summary>
public sealed class EnsureCreatedSchema : IDatabaseSchema
{
    public void Rebuild(TutuMuchDbContext db, TextWriter output)
    {
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        output.WriteLine("Schema created.");
    }
}
