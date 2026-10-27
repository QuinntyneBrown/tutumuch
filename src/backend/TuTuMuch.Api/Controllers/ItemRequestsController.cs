using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Api.Auth;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Requests;
using TuTuMuch.Domain.Requests;

namespace TuTuMuch.Api.Controllers;

/// <summary>
/// The signed-in member's item requests (L1-023): any signed-in member, verified or not, in every
/// launch phase; 401 for visitors (L2-105). The member comes from the session, never the request
/// (L2-010), so another member's request answers 404 (L2-099).
/// </summary>
[ApiController]
[Authorize]
public sealed class ItemRequestsController(ISender sender) : ControllerBase
{
    [HttpGet("api/me/item-requests")]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetMyItemRequestsQuery(User.MemberId()), cancellationToken));

    [HttpGet("api/me/item-requests/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ToResult(await sender.Send(new GetMyItemRequestQuery(User.MemberId(), id), cancellationToken));

    [HttpPost("api/me/item-requests")]
    public async Task<IActionResult> Create(ItemRequestBody body, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateItemRequestCommand(User.MemberId(), body.ToDetails()), cancellationToken);
        return result.IsSuccess ? Created($"/api/me/item-requests/{result.Value!.Id}", result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPut("api/me/item-requests/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ItemRequestBody body, CancellationToken cancellationToken) =>
        ToResult(await sender.Send(new UpdateItemRequestCommand(User.MemberId(), id, body.ToDetails()), cancellationToken));

    [HttpDelete("api/me/item-requests/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteItemRequestCommand(User.MemberId(), id), cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error!);
    }

    private IActionResult ToResult(Application.Common.Result<ItemRequestDto> result) =>
        result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
}

/// <summary>The fields of an item request (L2-097); unknown members are ignored (L2-049).</summary>
public sealed record ItemRequestBody(string? Category, string? Size, string? Keywords, decimal? MaxPrice, string? MinimumCondition)
{
    public ItemRequestDetails ToDetails() => new(Category, Size, Keywords, MaxPrice, MinimumCondition);
}
