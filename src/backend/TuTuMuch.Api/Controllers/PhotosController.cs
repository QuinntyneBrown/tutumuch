using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Application.Abstractions;

namespace TuTuMuch.Api.Controllers;

/// <summary>
/// Serves public photo versions by their content-addressed key with a one-year immutable cache
/// header (L2-059). A CDN in front of this origin, or object storage behind it, needs no change.
/// </summary>
[ApiController]
public sealed partial class PhotosController(IPhotoStorage storage) : ControllerBase
{
    [HttpGet("photos/{hash}/{file}")]
    public async Task<IActionResult> Get(string hash, string file, CancellationToken cancellationToken)
    {
        if (!KeyPattern().IsMatch($"{hash}/{file}"))
        {
            return NotFound();
        }

        var stored = await storage.ReadPublicAsync($"{hash}/{file}", cancellationToken);
        if (stored is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(stored.Content, stored.ContentType);
    }

    [GeneratedRegex("^[0-9a-f]{64}/[0-9]{3,4}\\.(webp|jpg)$")]
    private static partial Regex KeyPattern();
}
