using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutuMuch.Api.Auth;
using TutuMuch.Api.Http;
using TutuMuch.Application.Payouts;

namespace TutuMuch.Api.Controllers;

/// <summary>
/// The seller's payouts (L2-031): the summary of the payouts page and the link to the provider's
/// hosted onboarding. 401 for visitors; 502 PAYOUT_PROVIDER_UNAVAILABLE when the provider fails.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.VerifiedMember)]
public sealed class PayoutsController(ISender sender) : ControllerBase
{
    [HttpGet("api/payouts")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetPayoutSummaryQuery(User.MemberId()), cancellationToken));

    [HttpPost("api/payouts/onboarding-link")]
    public async Task<IActionResult> OnboardingLink(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateOnboardingLinkCommand(User.MemberId()), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}
