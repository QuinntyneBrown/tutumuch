using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TuTuMuch.Infrastructure.Persistence;

/// <summary>Lets dotnet-ef build the model to add migrations. It never opens a connection.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TuTuMuchDbContext>
{
    public TuTuMuchDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<TuTuMuchDbContext>().UseSqlServer("Server=design-time;Database=TuTuMuch").Options);
}
