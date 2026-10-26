using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TutuMuch.Infrastructure.Persistence;

/// <summary>Lets dotnet-ef build the model to add migrations. It never opens a connection.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TutuMuchDbContext>
{
    public TutuMuchDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<TutuMuchDbContext>().UseSqlServer("Server=design-time;Database=TutuMuch").Options);
}
