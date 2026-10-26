namespace TutuMuch.Infrastructure.Tests;

public sealed class AssemblyTests
{
    [Fact]
    public void Marker_type_belongs_to_the_Infrastructure_assembly()
    {
        Assert.Equal("TutuMuch.Infrastructure", typeof(InfrastructureAssembly).Assembly.GetName().Name);
    }
}
