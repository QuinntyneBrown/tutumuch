using System.Globalization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutuMuch.Api.Auth;
using TutuMuch.Api.Http;
using TutuMuch.Application.Notifications;

namespace TutuMuch.Api.Controllers;

/// <summary>
/// The signed-in member's email settings (L2-043). Any signed-in member may use them, verified or
/// not, because account security email reaches unverified members too.
/// </summary>
[ApiController]
[Authorize]
public sealed class NotificationPreferencesController(ISender sender) : ControllerBase
{
    [HttpGet("api/me/notification-preferences")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        WithEtag(await sender.Send(new GetNotificationPreferencesQuery(User.MemberId()), cancellationToken));

    /// <summary>Saves the two optional choices under the version in If-Match: 428 without it, 412 when it is old.</summary>
    [HttpPut("api/me/notification-preferences")]
    public async Task<IActionResult> Update(UpdateNotificationPreferencesRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateNotificationPreferencesCommand(User.MemberId(), request.ReviewEmails, request.MarketingEmails, IfMatchVersion());
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? WithEtag(result.Value!) : this.ToProblem(result.Error!);
    }

    private OkObjectResult WithEtag(NotificationPreferencesDto preferences)
    {
        Response.Headers.ETag = $"\"{preferences.Version.ToString(CultureInfo.InvariantCulture)}\"";
        Response.Headers.CacheControl = "no-store";
        return Ok(preferences);
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
/// Only the optional categories. There is no field for account security or order emails, so no
/// request can turn them off (L2-043); unknown members are ignored (L2-049).
/// </summary>
public sealed record UpdateNotificationPreferencesRequest(bool? ReviewEmails, bool? MarketingEmails);
