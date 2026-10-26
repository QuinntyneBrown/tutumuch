using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Api.Auth;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Orders;

namespace TuTuMuch.Api.Controllers;

/// <summary>
/// Order history and actions (L2-032 to L2-035, L2-037): the member lists their purchases and
/// sales, newest first; the seller reads the sale, adds tracking, cancels and answers
/// a request to cancel; the buyer confirms receipt and asks to cancel. 401 for visitors, 404 for
/// anyone but the order's seller or buyer, 409 ORDER_INVALID_TRANSITION when the order's status
/// does not allow the action.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.VerifiedMember)]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpGet("api/purchases")]
    public async Task<IActionResult> Purchases(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ListOrdersQuery(User.MemberId(), OrderRole.Buyer), cancellationToken));

    [HttpGet("api/sales")]
    public async Task<IActionResult> Sales(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ListOrdersQuery(User.MemberId(), OrderRole.Seller), cancellationToken));

    [HttpGet("api/sales/{id:guid}")]
    public async Task<IActionResult> Sale(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSaleQuery(User.MemberId(), id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPost("api/orders/{id:guid}/shipment")]
    public async Task<IActionResult> AddTracking(Guid id, AddTrackingRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddTrackingCommand(User.MemberId(), id, request.Carrier, request.TrackingNumber), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPost("api/orders/{id:guid}/receipt")]
    public async Task<IActionResult> ConfirmReceipt(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmReceiptCommand(User.MemberId(), id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPost("api/orders/{id:guid}/cancellation")]
    public async Task<IActionResult> Cancel(Guid id, CancelOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelOrderCommand(User.MemberId(), id, request.Reason, request.Note), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPost("api/orders/{id:guid}/cancellation-request")]
    public async Task<IActionResult> RequestCancellation(Guid id, CancellationRequestRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RequestCancellationCommand(User.MemberId(), id, request.Message), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPost("api/orders/{id:guid}/cancellation-request/response")]
    public async Task<IActionResult> RespondToCancellation(Guid id, CancellationResponseRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RespondToCancellationCommand(User.MemberId(), id, request.Decision), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}

public sealed record AddTrackingRequest(string? Carrier, string? TrackingNumber);

public sealed record CancelOrderRequest(string? Reason, string? Note);

public sealed record CancellationRequestRequest(string? Message);

public sealed record CancellationResponseRequest(string? Decision);
