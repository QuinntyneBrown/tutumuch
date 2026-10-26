using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutuMuch.Api.Auth;
using TutuMuch.Api.Http;
using TutuMuch.Application.Orders;

namespace TutuMuch.Api.Controllers;

/// <summary>
/// The administrator's side of an order problem (L2-036): read it and decide it. The only
/// administrator feature; 403 ADMINISTRATOR_REQUIRED without the role (L2-010 AC3).
/// </summary>
[ApiController]
[Authorize(Policy = Policies.Administrator)]
public sealed class AdminProblemsController(ISender sender) : ControllerBase
{
    [HttpGet("api/admin/problems/{reference}")]
    public async Task<IActionResult> Get(string reference, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProblemDetailQuery(reference), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPost("api/admin/problems/{reference}/resolution")]
    public async Task<IActionResult> Resolve(string reference, ResolutionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResolveProblemCommand(User.MemberId(), reference, request.Outcome, request.Reason), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}

public sealed record ResolutionRequest(string? Outcome, string? Reason);
