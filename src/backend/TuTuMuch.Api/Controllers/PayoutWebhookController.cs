using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Payouts;

namespace TuTuMuch.Api.Controllers;

/// <summary>
/// The provider's payout webhook: account.updated, payout.paid and payout.failed (L2-031).
/// Anonymous: the signature over the raw body is the authentication. An invalid signature is 400
/// and logged as a security event; processed and repeated events are 200.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class PayoutWebhookController(ISender sender, ILogger<PayoutWebhookController> logger) : ControllerBase
{
    public const string SignatureHeader = "Payout-Signature";

    [HttpPost("api/payouts/webhook")]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers[SignatureHeader].FirstOrDefault();
        var result = await sender.Send(new HandlePayoutWebhookCommand(body, signature), cancellationToken);
        if (!result.IsSuccess)
        {
            logger.LogWarning("Security event {SecurityEvent} from {RemoteIp}", "payout.webhook.invalid_signature", HttpContext.Connection.RemoteIpAddress);
            return this.ToProblem(result.Error!);
        }

        return Ok();
    }
}
