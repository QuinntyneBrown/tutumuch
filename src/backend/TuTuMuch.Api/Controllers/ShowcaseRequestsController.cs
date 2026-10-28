using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Api.Auth;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Requests;

namespace TuTuMuch.Api.Controllers;

/// <summary>
/// The signed-in member's request for a showcase listing (L2-109): any signed-in member, verified
/// or not, in every launch phase; 401 for visitors. The member comes from the session (L2-010), so
/// another member's request answers 404 (L2-111).
/// </summary>
[ApiController]
[Authorize]
public sealed class ShowcaseRequestsController(ISender sender) : ControllerBase
{
    [HttpGet("api/me/showcase-requests/{reference}")]
    public async Task<IActionResult> Get(string reference, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyShowcaseRequestQuery(User.MemberId(), reference), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    /// <summary>201 for a new request, 200 when the member's request for this listing was updated.</summary>
    [HttpPut("api/me/showcase-requests/{reference}")]
    public async Task<IActionResult> Send(string reference, ShowcaseRequestBody body, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SendShowcaseRequestCommand(User.MemberId(), reference, body.Note), cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error!);
        }

        return result.Value!.Created
            ? Created($"/api/me/showcase-requests/{reference}", result.Value.Request)
            : Ok(result.Value.Request);
    }
}

/// <summary>
/// The administrator's side of showcase listings (L2-107, L2-110): mark a listing and read every
/// request. 403 ADMINISTRATOR_REQUIRED without the role (L2-010 AC3).
/// </summary>
[ApiController]
[Authorize(Policy = Policies.Administrator)]
public sealed class AdminShowcaseController(ISender sender) : ControllerBase
{
    [HttpPut("api/admin/listings/{reference}/showcase")]
    public async Task<IActionResult> SetShowcase(string reference, ShowcaseBody body, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetShowcaseCommand(User.MemberId(), reference, body.Showcase), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpGet("api/admin/showcase-requests")]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetShowcaseRequestsQuery(), cancellationToken));
}

/// <summary>The optional note (L2-109); unknown members are ignored (L2-049).</summary>
public sealed record ShowcaseRequestBody(string? Note);

public sealed record ShowcaseBody(bool Showcase);
