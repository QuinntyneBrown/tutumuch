using System.Globalization;
using MediatR;
using Microsoft.Extensions.Options;
using TuTuMuch.Domain.Launch;

namespace TuTuMuch.Application.Launch;

/// <summary>
/// The Launch section (L2-089, ADR platform/0014). Phase is read as text so that "two" is refused
/// with the same message as "4"; missing or empty means Phase 1.
/// </summary>
public sealed class LaunchOptions
{
    public const string Section = "Launch";

    public string? Phase { get; set; }

    /// <summary>In Phase 1 the catalog holds fewer listings than this, in any status (L2-092).</summary>
    public int PhaseOneProductLimit { get; set; } = 40;

    public LaunchPhase CurrentPhase => TryParsePhase(Phase, out var phase) ? phase : LaunchPhase.One;

    public static bool TryParsePhase(string? value, out LaunchPhase phase)
    {
        phase = LaunchPhase.One;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var number) && Enum.IsDefined((LaunchPhase)number))
        {
            phase = (LaunchPhase)number;
            return true;
        }

        return false;
    }
}

/// <summary>Refuses a phase other than 1, 2 or 3 and a limit below 1; with ValidateOnStart the host does not start.</summary>
public sealed class LaunchOptionsValidator : IValidateOptions<LaunchOptions>
{
    public ValidateOptionsResult Validate(string? name, LaunchOptions options)
    {
        var failures = new List<string>();
        if (!LaunchOptions.TryParsePhase(options.Phase, out _))
        {
            failures.Add($"Launch:Phase must be 1, 2 or 3 (found '{options.Phase}').");
        }

        if (options.PhaseOneProductLimit < 1)
        {
            failures.Add("Launch:PhaseOneProductLimit must be 1 or more.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>The phase as the web application needs it (L2-090).</summary>
public sealed record LaunchDto(int Phase, bool CategoryMenusVisible, bool MemberSellingOpen);

public sealed record GetLaunchQuery : IRequest<LaunchDto>;

public sealed class GetLaunchQueryHandler(IOptions<LaunchOptions> options) : IRequestHandler<GetLaunchQuery, LaunchDto>
{
    public Task<LaunchDto> Handle(GetLaunchQuery query, CancellationToken cancellationToken)
    {
        var phase = options.Value.CurrentPhase;
        return Task.FromResult(new LaunchDto((int)phase, LaunchRules.CategoryMenusVisible(phase), LaunchRules.MemberSellingOpen(phase)));
    }
}
