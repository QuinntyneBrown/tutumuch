using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutuMuch.Api.Http;
using TutuMuch.Application.Profiles;

namespace TutuMuch.Api.Controllers;

/// <summary>Public member profiles: anyone may read them, signed in or not (L2-041).</summary>
[ApiController]
[AllowAnonymous]
public sealed class MembersController(ISender sender) : ControllerBase
{
    /// <summary>The profile header, stats and rating summary. Unknown and closed members are one 404.</summary>
    [HttpGet("api/members/{displayName}")]
    public async Task<IActionResult> Profile(string displayName, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPublicProfileQuery(displayName), cancellationToken);
        Response.Headers.CacheControl = result.IsSuccess ? "public, max-age=60" : "no-store";
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    /// <summary>The member's Active listings, newest first, 24 per page.</summary>
    [HttpGet("api/members/{displayName}/listings")]
    public async Task<IActionResult> Listings(string displayName, int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMemberListingsQuery(displayName, page), cancellationToken);
        Response.Headers.CacheControl = result.IsSuccess ? "public, max-age=60" : "no-store";
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}
