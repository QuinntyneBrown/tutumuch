using System.Globalization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Api.Auth;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Listings;
using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Api.Controllers;

/// <summary>
/// Create, change, publish and withdraw the caller's listings, and list them all by status (My listings, L2-021). Every action needs a verified member
/// (CanSell, L2-003, L2-010): 401 for visitors, 403 EMAIL_NOT_VERIFIED for unverified members.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.CanSell)]
public sealed class ListingsController(ISender sender) : ControllerBase
{
    [HttpPost("api/listings")]
    public async Task<IActionResult> Create(ListingDetailsRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateListingDraftCommand(User.MemberId(), request.ToDetails(), request.PhotoIds), cancellationToken);
        return result.IsSuccess
            ? Created($"/api/me/listings/{result.Value!.Reference}", result.Value)
            : this.ToProblem(result.Error!);
    }

    [HttpPut("api/listings/{reference}")]
    public async Task<IActionResult> Update(string reference, ListingDetailsRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateListingCommand(User.MemberId(), reference, request.ToDetails(), request.PhotoIds, request.SizeConfirmed ?? false, IfMatchVersion());
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? WithEtag(result.Value!) : this.ToProblem(result.Error!);
    }

    [HttpPost("api/listings/{reference}/withdraw")]
    public async Task<IActionResult> Withdraw(string reference, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new WithdrawListingCommand(User.MemberId(), reference), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPost("api/listings/{reference}/publish")]
    public async Task<IActionResult> Publish(string reference, PublishRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishListingCommand(User.MemberId(), reference, request.SizeConfirmed), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPost("api/listings/{reference}/relist")]
    public async Task<IActionResult> Relist(string reference, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RelistListingCommand(User.MemberId(), reference), cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error!);
    }

    [HttpGet("api/me/listings")]
    public async Task<IActionResult> Mine([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyListingsQuery(User.MemberId(), status), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpGet("api/me/listings/{reference}")]
    public async Task<IActionResult> GetMine(string reference, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyListingQuery(User.MemberId(), reference), cancellationToken);
        return result.IsSuccess ? WithEtag(result.Value!) : this.ToProblem(result.Error!);
    }

    /// <summary>The listing with its version as a strong ETag, "\"7\"", for the edit form to send back in If-Match.</summary>
    private OkObjectResult WithEtag(MyListingDto listing)
    {
        Response.Headers.ETag = $"\"{listing.Version.ToString(CultureInfo.InvariantCulture)}\"";
        return Ok(listing);
    }

    /// <summary>The version in If-Match; null when the header is missing, -1 (never current) when it is not one of our ETags.</summary>
    private int? IfMatchVersion()
    {
        var header = Request.Headers.IfMatch.ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        var tag = header.Trim().Trim('"');
        return int.TryParse(tag, NumberStyles.None, CultureInfo.InvariantCulture, out var version) ? version : -1;
    }
}

/// <summary>
/// The editable fields. There is no seller, status, confirmation time or timestamp member, so a
/// client cannot set them (mass assignment, L2-049); unknown members are ignored. PhotoIds lists
/// uploaded photos in display order (L2-017); leaving it out keeps the current photos. SizeConfirmed
/// is the seller ticking the size confirmation again when editing a published listing (L2-014).
/// </summary>
public sealed record ListingDetailsRequest(
    string? Category,
    string? Subcategory,
    string? Size,
    string? Title,
    string? Description,
    string? Brand,
    string? Condition,
    string? Flaws,
    decimal? Price,
    decimal? ShippingCost,
    IReadOnlyList<Guid>? PhotoIds = null,
    bool? SizeConfirmed = null)
{
    public ListingDetails ToDetails() => new(Category, Subcategory, Size, Title, Description, Brand, Condition, Flaws, Price, ShippingCost);
}

public sealed record PublishRequest(bool SizeConfirmed);
