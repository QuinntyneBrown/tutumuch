using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Api.Auth;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Checkouts;
using TuTuMuch.Application.Common;
using TuTuMuch.Application.Grabs;
using TuTuMuch.Domain.Finds;

namespace TuTuMuch.Api.Controllers;

/// <summary>
/// The shopper's grabs (L2-117, L2-119): a verified member asks TuTuMuch to grab a find, and reads,
/// cancels or receives their own grabs. 401 for visitors, 403 EMAIL_NOT_VERIFIED for unverified
/// members, 404 for anyone but the shopper (L2-121).
/// </summary>
[ApiController]
[Authorize(Policy = Policies.VerifiedMember)]
public sealed class GrabsController(ISender sender) : ControllerBase
{
    private static readonly Error KeyRequired = new(400, "IDEMPOTENCY_KEY_REQUIRED", "Send an Idempotency-Key header with a UUID");

    [HttpPost("api/grabs/quote")]
    public async Task<IActionResult> Quote(GrabQuoteBody body, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new QuoteGrabCommand(User.MemberId(), body.FindReference ?? string.Empty, body.Address), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    /// <summary>The body carries only the provider's payment method token: no card data reaches TuTuMuch (L2-030).</summary>
    [HttpPost("api/grabs")]
    public async Task<IActionResult> Start(StartGrabBody body, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(Request.Headers[CheckoutsController.IdempotencyKeyHeader].ToString(), out var key))
        {
            return this.ToProblem(KeyRequired);
        }

        var result = await sender.Send(
            new StartGrabCommand(User.MemberId(), body.FindReference ?? string.Empty, body.Address, body.DepositAcknowledged, body.PaymentMethodToken, body.ExpectedTotal, key),
            cancellationToken);
        return result.IsSuccess ? Created($"/api/me/grabs/{result.Value!.Id}", result.Value) : this.ToProblem(result.Error!);
    }

    [HttpGet("api/me/grabs")]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetMyGrabsQuery(User.MemberId()), cancellationToken));

    [HttpGet("api/me/grabs/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyGrabQuery(User.MemberId(), id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPost("api/me/grabs/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelGrabCommand(User.MemberId(), id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPost("api/me/grabs/{id:guid}/received")]
    public async Task<IActionResult> Received(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmGrabReceivedCommand(User.MemberId(), id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}

/// <summary>
/// TuTuMuch handles grabs (L2-118): the list and the four actions, behind the Administrator policy;
/// 403 ADMINISTRATOR_REQUIRED without the role (L2-010 AC3).
/// </summary>
[ApiController]
[Authorize(Policy = Policies.Administrator)]
public sealed class AdminGrabsController(ISender sender) : ControllerBase
{
    [HttpGet("api/admin/grabs")]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetAdminGrabsQuery(), cancellationToken));

    [HttpPost("api/admin/grabs/{id:guid}/accept")]
    public Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken) => ActAsync(new ActOnGrabCommand(id, GrabAction.Accept), cancellationToken);

    [HttpPost("api/admin/grabs/{id:guid}/decline")]
    public Task<IActionResult> Decline(Guid id, CancellationToken cancellationToken) => ActAsync(new ActOnGrabCommand(id, GrabAction.Decline), cancellationToken);

    [HttpPost("api/admin/grabs/{id:guid}/bought")]
    public Task<IActionResult> Bought(Guid id, CancellationToken cancellationToken) => ActAsync(new ActOnGrabCommand(id, GrabAction.Bought), cancellationToken);

    /// <summary>The reason is "Gone" or "PriceWentUp"; anything else is refused with 422.</summary>
    [HttpPost("api/admin/grabs/{id:guid}/unavailable")]
    public Task<IActionResult> Unavailable(Guid id, UnavailableBody body, CancellationToken cancellationToken) =>
        ActAsync(new ActOnGrabCommand(id, GrabAction.Unavailable, Enum.TryParse<GrabUnavailableReason>(body.Reason, out var reason) && Enum.IsDefined(reason) ? reason : null), cancellationToken);

    private async Task<IActionResult> ActAsync(ActOnGrabCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}

public sealed record GrabQuoteBody(string? FindReference, AddressInput? Address);

public sealed record StartGrabBody(string? FindReference, AddressInput? Address, bool DepositAcknowledged, string? PaymentMethodToken, decimal ExpectedTotal);

public sealed record UnavailableBody(string? Reason);
