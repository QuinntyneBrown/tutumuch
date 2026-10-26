using MediatR;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Application.Listings;

namespace TuTuMuch.Api.Controllers;

/// <summary>The accepted categories and the selling options, open to anyone (L2-011, L2-031).</summary>
[ApiController]
[Route("api")]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "public, max-age=3600";
        return Ok(await sender.Send(new GetCategoriesQuery(), cancellationToken));
    }

    [HttpGet("selling/options")]
    public async Task<IActionResult> SellingOptions(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetSellingOptionsQuery(), cancellationToken));
}
