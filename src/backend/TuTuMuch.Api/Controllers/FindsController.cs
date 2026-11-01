using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Api.Auth;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Finds;
using TuTuMuch.Domain.Finds;

namespace TuTuMuch.Api.Controllers;

/// <summary>
/// Shelf finds (L2-113 to L2-116, ADR catalog/0009). Anyone may browse and read them; posting needs
/// the CanSell policy of the launch phase (L2-091); marking gone and withdrawing need the poster.
/// </summary>
[ApiController]
public sealed class FindsController(ISender sender) : ControllerBase
{
    [HttpGet("api/finds")]
    [AllowAnonymous]
    public async Task<IActionResult> List(int page = 1, int? pageSize = null, CancellationToken cancellationToken = default)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await sender.Send(new BrowseFindsQuery(page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    /// <summary>The session is read when present so the poster is recognised; a visitor is never 401.</summary>
    [HttpGet("api/finds/{reference}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(string reference, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        Guid? viewerId = User.Identity?.IsAuthenticated == true ? User.MemberId() : null;
        var result = await sender.Send(new GetFindQuery(reference, viewerId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPost("api/finds")]
    [Authorize(Policy = Policies.CanSell)]
    public async Task<IActionResult> Post(PostFindBody body, CancellationToken cancellationToken)
    {
        var details = new ShelfFindDetails(body.Title, body.Category, body.ShelfPrice, body.StoreName, body.StoreAddress, body.SeenOn, body.Note, body.GrabFee);
        var result = await sender.Send(new PostFindCommand(User.MemberId(), details), cancellationToken);
        return result.IsSuccess ? Created($"/api/finds/{result.Value!.Reference}", result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPost("api/finds/{reference}/gone")]
    [Authorize]
    public async Task<IActionResult> MarkGone(string reference, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkFindGoneCommand(User.MemberId(), reference), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpDelete("api/finds/{reference}")]
    [Authorize]
    public async Task<IActionResult> Withdraw(string reference, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new WithdrawFindCommand(User.MemberId(), reference), cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error!);
    }
}

/// <summary>A find as the poster sends it (L2-113); unknown members are ignored (L2-049).</summary>
public sealed record PostFindBody(
    string? Title,
    string? Category,
    decimal? ShelfPrice,
    string? StoreName,
    string? StoreAddress,
    DateOnly? SeenOn,
    string? Note,
    decimal? GrabFee);
