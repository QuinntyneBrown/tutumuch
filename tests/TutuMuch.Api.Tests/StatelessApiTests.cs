// Architecture Test
// Traces to: L2-060
// Description: The API and application layers keep no per-request or per-member state in process memory.

using NetArchTest.Rules;

namespace TutuMuch.Api.Tests;

public sealed class StatelessApiTests
{
    private static readonly string[] ForbiddenDependencies =
    [
        "Microsoft.Extensions.Caching.Memory",
        "Microsoft.AspNetCore.Session",
    ];

    [Theory]
    [InlineData(typeof(Program))]
    [InlineData(typeof(TutuMuch.Application.ApplicationAssembly))]
    public void Layer_does_not_depend_on_in_process_state(Type marker)
    {
        var result = Types.InAssembly(marker.Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ForbiddenDependencies)
            .GetResult();

        Assert.True(result.IsSuccessful, "In-process state found in: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
