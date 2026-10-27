using MediatR;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Application.Launch;

namespace TuTuMuch.Api.Controllers;

/// <summary>The launch phase for the web application, open to anyone (L2-090, ADR platform/0014).</summary>
[ApiController]
[Route("api/launch")]
public sealed class LaunchController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "public, max-age=60";
        return Ok(await sender.Send(new GetLaunchQuery(), cancellationToken));
    }
}
