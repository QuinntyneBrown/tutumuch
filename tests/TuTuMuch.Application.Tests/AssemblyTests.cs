namespace TuTuMuch.Application.Tests;

public sealed class AssemblyTests
{
    [Fact]
    public void Marker_type_belongs_to_the_Application_assembly()
    {
        Assert.Equal("TuTuMuch.Application", typeof(ApplicationAssembly).Assembly.GetName().Name);
    }
}
