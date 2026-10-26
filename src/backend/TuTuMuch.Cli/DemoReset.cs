using Microsoft.Extensions.Options;
using TuTuMuch.Infrastructure.Persistence;
using TuTuMuch.Infrastructure.Photos;

namespace TuTuMuch.Cli;

/// <summary>Gives a fresh, empty install: the schema rebuilt and the photo store emptied (L2-072).</summary>
public sealed class DemoReset(TuTuMuchDbContext db, IDatabaseSchema schema, IOptions<PhotoStorageOptions> photoStorage)
{
    public void Run(TextWriter output)
    {
        schema.Rebuild(db, output);
        db.ChangeTracker.Clear();
        var root = photoStorage.Value.RootPath;
        foreach (var area in new[] { "public", "private" })
        {
            var folder = Path.Combine(root, area);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }

        output.WriteLine($"Photo store emptied ({root}).");
    }
}
