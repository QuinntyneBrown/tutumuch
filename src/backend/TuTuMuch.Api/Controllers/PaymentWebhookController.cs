using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Checkouts;

namespace TuTuMuch.Api.Controllers;

/// <summary>
/// The payment provider's webhook (L2-030). Anonymous: the signature over the raw body is the
/// authentication. An invalid signature is 400 and logged as a security event; processed and
/// repeated events are 200.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class PaymentWebhookController(ISender sender, ILogger<PaymentWebhookController> logger) : ControllerBase
{
    public const string SignatureHeader = "Payment-Signature";

    [HttpPost("api/payments/webhook")]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers[SignatureHeader].FirstOrDefault();
        var result = await sender.Send(new HandlePaymentWebhookCommand(body, signature), cancellationToken);
        if (!result.IsSuccess)
        {
            logger.LogWarning("Security event {SecurityEvent} from {RemoteIp}", "payment.webhook.invalid_signature", HttpContext.Connection.RemoteIpAddress);
            return this.ToProblem(result.Error!);
        }

        return Ok();
    }
}
