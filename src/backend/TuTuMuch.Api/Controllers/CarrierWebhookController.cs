using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Orders;

namespace TuTuMuch.Api.Controllers;

/// <summary>
/// The carrier tracking service's webhook (L2-033 AC4). Anonymous: the signature over the raw body
/// is the authentication. An invalid signature is 400 and logged as a security event; delivered,
/// repeated and unrelated events are 200.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class CarrierWebhookController(ISender sender, ILogger<CarrierWebhookController> logger) : ControllerBase
{
    public const string SignatureHeader = "Carrier-Signature";

    [HttpPost("api/carriers/webhook")]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers[SignatureHeader].FirstOrDefault();
        var result = await sender.Send(new HandleCarrierWebhookCommand(body, signature), cancellationToken);
        if (!result.IsSuccess)
        {
            logger.LogWarning("Security event {SecurityEvent} from {RemoteIp}", "carrier.webhook.invalid_signature", HttpContext.Connection.RemoteIpAddress);
            return this.ToProblem(result.Error!);
        }

        return Ok();
    }
}
