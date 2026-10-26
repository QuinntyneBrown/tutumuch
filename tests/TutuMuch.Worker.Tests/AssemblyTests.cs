namespace TutuMuch.Worker.Tests;

public sealed class AssemblyTests
{
    [Fact]
    public void Worker_type_belongs_to_the_worker_assembly()
    {
        Assert.Equal("TutuMuch.Worker", typeof(TutuMuch.Worker.Worker).Assembly.GetName().Name);
    }
}
