namespace TutuMuch.Domain.Tests;

public sealed class AssemblyTests
{
    [Fact]
    public void Marker_type_belongs_to_the_Domain_assembly()
    {
        Assert.Equal("TutuMuch.Domain", typeof(DomainAssembly).Assembly.GetName().Name);
    }
}
