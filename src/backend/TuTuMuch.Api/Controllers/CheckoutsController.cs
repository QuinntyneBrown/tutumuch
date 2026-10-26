using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Api.Auth;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Checkouts;
using TuTuMuch.Application.Common;

namespace TuTuMuch.Api.Controllers;

/// <summary>
/// Checkout for a verified member (L2-027 to L2-030): reserve, address, pay and release. 401 for
/// visitors, 403 EMAIL_NOT_VERIFIED for unverified members, 404 for anyone but the buyer.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.VerifiedMember)]
public sealed class CheckoutsController(ISender sender) : ControllerBase
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    private static readonly Error KeyRequired = new(400, "IDEMPOTENCY_KEY_REQUIRED", "Send an Idempotency-Key header with a UUID");

    [HttpPost("api/checkouts")]
    public async Task<IActionResult> Start(StartCheckoutRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new StartCheckoutCommand(User.MemberId(), request.ListingReference ?? string.Empty), cancellationToken);
        return result.IsSuccess
            ? Created($"/api/checkouts/{result.Value!.Id}", result.Value)
            : this.ToProblem(result.Error!);
    }

    [HttpGet("api/checkouts/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCheckoutQuery(User.MemberId(), id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPut("api/checkouts/{id:guid}/address")]
    public async Task<IActionResult> SetAddress(Guid id, AddressInput request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetCheckoutAddressCommand(User.MemberId(), id, request), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    /// <summary>The body carries only the provider's payment method token: no card data reaches TuTuMuch (L2-030).</summary>
    [HttpPost("api/checkouts/{id:guid}/payments")]
    public async Task<IActionResult> Pay(Guid id, PaymentRequestBody request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(Request.Headers[IdempotencyKeyHeader].ToString(), out var key) || string.IsNullOrWhiteSpace(request.PaymentMethodToken))
        {
            return this.ToProblem(KeyRequired);
        }

        var result = await sender.Send(new SubmitPaymentCommand(User.MemberId(), id, key, request.PaymentMethodToken, request.ExpectedTotal), cancellationToken);
        return result.IsSuccess ? Accepted(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpDelete("api/checkouts/{id:guid}")]
    public async Task<IActionResult> Release(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReleaseCheckoutCommand(User.MemberId(), id), cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error!);
    }

    [HttpGet("api/purchases/{id:guid}")]
    public async Task<IActionResult> Purchase(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPurchaseQuery(User.MemberId(), id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}

public sealed record StartCheckoutRequest(string? ListingReference);

/// <summary>The provider's token for the card entered in its hosted fields and the total the buyer saw.</summary>
public sealed record PaymentRequestBody(string PaymentMethodToken, decimal ExpectedTotal);
