using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Api.Auth;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Listings;

namespace TuTuMuch.Api.Controllers;

/// <summary>
/// Bids on an Active auction (L2-115, L2-116). Any verified member but the seller may bid in every
/// launch phase: 401 for visitors, 403 EMAIL_NOT_VERIFIED for unverified members.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.VerifiedMember)]
public sealed class BidsController(ISender sender) : ControllerBase
{
    [HttpPost("api/listings/{reference}/bids")]
    public async Task<IActionResult> Place(string reference, PlaceBidRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PlaceBidCommand(User.MemberId(), reference, request.Amount ?? 0m), cancellationToken);
        return result.IsSuccess
            ? Created($"/api/listings/{reference}", result.Value)
            : this.ToProblem(result.Error!);
    }
}

/// <summary>The amount in dollars, at most two decimal places; the bidder is the caller, never the body (L2-049).</summary>
public sealed record PlaceBidRequest(decimal? Amount);
