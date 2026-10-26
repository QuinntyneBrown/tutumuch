using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutuMuch.Api.Auth;
using TutuMuch.Api.Http;
using TutuMuch.Application.Listings;
using TutuMuch.Application.Orders;

namespace TutuMuch.Api.Controllers;

/// <summary>
/// The members' side of an order problem (L2-036): the buyer reports it with up to 4 photos, the
/// seller replies once, and the order's members and administrators read the private photos. 404
/// for anyone else (L2-010).
/// </summary>
[ApiController]
[Authorize(Policy = Policies.VerifiedMember)]
public sealed class OrderProblemsController(ISender sender) : ControllerBase
{
    /// <summary>Four photos at the 10 MB limit, plus the form fields.</summary>
    public const long BodyLimit = 4 * PhotoUploadsController.BodyLimit;

    [HttpPost("api/orders/{id:guid}/problem")]
    [RequestSizeLimit(BodyLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = BodyLimit)]
    public async Task<IActionResult> Report(Guid id, [FromForm] string? reason, [FromForm] string? description, [FromForm] List<IFormFile>? photos, CancellationToken cancellationToken)
    {
        var uploads = new List<ProblemPhotoUpload>();
        foreach (var photo in photos ?? [])
        {
            using var buffer = new MemoryStream();
            await photo.CopyToAsync(buffer, cancellationToken);
            uploads.Add(new ProblemPhotoUpload(buffer.ToArray()));
        }

        var result = await sender.Send(new ReportProblemCommand(User.MemberId(), id, reason, description, uploads), cancellationToken);
        return result.IsSuccess ? Created($"/api/orders/{id}/problem", result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPost("api/orders/{id:guid}/problem/response")]
    public async Task<IActionResult> Respond(Guid id, ProblemResponseRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RespondToProblemCommand(User.MemberId(), id, request.Message), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpGet("api/orders/{id:guid}/problem/photos/{photoId:guid}")]
    public async Task<IActionResult> Photo(Guid id, Guid photoId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProblemPhotoQuery(User.MemberId(), id, photoId), cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error!);
        }

        Response.Headers.CacheControl = "private, no-store";
        return File(result.Value!.Content, result.Value.ContentType);
    }
}

public sealed record ProblemResponseRequest(string? Message);
