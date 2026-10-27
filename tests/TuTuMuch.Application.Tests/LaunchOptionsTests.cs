// Unit Test
// Traces to: L2-089
// Description: Launch:Phase is 1, 2 or 3 and means Phase 1 when missing; any other value and a
// Phase 1 product limit below 1 are refused with a clear message.

using TuTuMuch.Application.Launch;
using TuTuMuch.Domain.Launch;

namespace TuTuMuch.Application.Tests;

public sealed class LaunchOptionsTests
{
    private readonly LaunchOptionsValidator _validator = new();

    [Fact]
    public void A_missing_section_means_phase_1_with_a_limit_of_40()
    {
        var options = new LaunchOptions();

        Assert.True(_validator.Validate(null, options).Succeeded);
        Assert.Equal(LaunchPhase.One, options.CurrentPhase);
        Assert.Equal(40, options.PhaseOneProductLimit);
    }

    [Theory]
    [InlineData("1", LaunchPhase.One)]
    [InlineData("2", LaunchPhase.Two)]
    [InlineData(" 3 ", LaunchPhase.Three)]
    [InlineData("", LaunchPhase.One)]
    public void Phases_1_to_3_are_accepted(string value, LaunchPhase phase)
    {
        var options = new LaunchOptions { Phase = value };

        Assert.True(_validator.Validate(null, options).Succeeded);
        Assert.Equal(phase, options.CurrentPhase);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("4")]
    [InlineData("two")]
    [InlineData("1.5")]
    [InlineData("-1")]
    public void Any_other_phase_is_refused_with_a_clear_message(string value)
    {
        var result = _validator.Validate(null, new LaunchOptions { Phase = value });

        Assert.True(result.Failed);
        Assert.Contains("Launch:Phase must be 1, 2 or 3", result.FailureMessage);
    }

    [Fact]
    public void A_product_limit_below_1_is_refused()
    {
        var result = _validator.Validate(null, new LaunchOptions { PhaseOneProductLimit = 0 });

        Assert.True(result.Failed);
        Assert.Contains("Launch:PhaseOneProductLimit must be 1 or more", result.FailureMessage);
    }
}
