using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Notifications;

namespace TuTuMuch.Api.Controllers;

/// <summary>
/// The one-click marketing unsubscribe (L2-043), with no session: from TuTuMuch Web's unsubscribe page
/// (token in a JSON body) or a mail client's RFC 8058 POST to the List-Unsubscribe URL (token in the
/// query string, body <c>List-Unsubscribe=One-Click</c> as a form). It uses no cookie
/// authentication, so it needs no anti-forgery token.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class UnsubscribeController(ISender sender) : ControllerBase
{
    [HttpPost("api/notifications/unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromQuery] string? token, CancellationToken cancellationToken)
    {
        if (token is null && Request.HasJsonContentType())
        {
            try
            {
                token = (await Request.ReadFromJsonAsync<UnsubscribeRequest>(cancellationToken))?.Token;
            }
            catch (System.Text.Json.JsonException)
            {
                token = null;
            }
        }

        var result = await sender.Send(new UnsubscribeCommand(token), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}

public sealed record UnsubscribeRequest(string? Token);
