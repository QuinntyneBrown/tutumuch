using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Api.Auth;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Listings;

namespace TuTuMuch.Api.Controllers;

/// <summary>
/// Upload listing photos one file per request (L2-017). Needs a verified member (CanSell, L2-010).
/// The body limit sits above the 10 MB photo limit, so an oversized photo reaches the handler and
/// is refused with PHOTO_TOO_LARGE instead of a bare 413.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.CanSell)]
public sealed class PhotoUploadsController(ISender sender) : ControllerBase
{
    public const long BodyLimit = 16 * 1024 * 1024;

    [HttpPost("api/photo-uploads")]
    [RequestSizeLimit(BodyLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = BodyLimit)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return this.ToProblem(ListingPhotoHandlers.Unsupported);
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        var result = await sender.Send(new UploadListingPhotoCommand(User.MemberId(), buffer.ToArray()), cancellationToken);
        return result.IsSuccess
            ? Created($"/api/photo-uploads/{result.Value!.Id}", result.Value)
            : this.ToProblem(result.Error!);
    }

    [HttpGet("api/photo-uploads/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPhotoUploadQuery(User.MemberId(), id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}
