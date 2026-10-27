using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TuTuMuch.Api.Auth;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Accounts;
using TuTuMuch.Application.Common;
using TuTuMuch.Domain.Members;

namespace TuTuMuch.Api.Controllers;

/// <summary>
/// Sign in with Google (L2-088, ADR accounts/0001): the server-side authorization code flow with
/// PKCE, then the pending and finish steps of a new person's join. Every action answers 404 when
/// no Google client is configured.
/// </summary>
[ApiController]
[Route("api/auth/google")]
public sealed class GoogleSignInController(
    ISender sender,
    IGoogleIdentityProvider google,
    IOptions<GoogleOptions> options,
    GoogleFlowCookie flowCookie,
    SessionCookies cookies,
    TimeProvider clock) : ControllerBase
{
    public const string JoinCookie = "tl_google_join";
    public const string CallbackPath = "/api/auth/google/callback";
    private static readonly Error NotConfigured = Error.NotFound("GOOGLE_NOT_CONFIGURED");

    [HttpGet("start")]
    public IActionResult Start([FromQuery] string? returnUrl)
    {
        if (google.ClientId is null)
        {
            return this.ToProblem(NotConfigured);
        }

        var flow = new GoogleFlow(SecureTokens.Create(), SecureTokens.Create(), SecureTokens.Create(), SafeReturnUrl(returnUrl), clock.GetUtcNow().UtcDateTime);
        flowCookie.Write(Response, flow);
        var challenge = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(flow.CodeVerifier)));
        return Redirect(GoogleAuthorization.Url(google.ClientId, RedirectUri(), flow.State, flow.Nonce, challenge));
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? state, [FromQuery] string? code, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        if (google.ClientId is null)
        {
            return this.ToProblem(NotConfigured);
        }

        var flow = flowCookie.Read(Request);
        GoogleFlowCookie.Clear(Response);
        if (flow is null || string.IsNullOrEmpty(state) || !FixedTimeEquals(state, flow.State))
        {
            return LocalRedirect(Failed());
        }

        if (!string.IsNullOrEmpty(error))
        {
            return LocalRedirect("/sign-in?google=cancelled");
        }

        var result = await sender.Send(new GoogleCallbackCommand(code, flow.CodeVerifier, RedirectUri(), flow.Nonce), cancellationToken);
        if (!result.IsSuccess)
        {
            return LocalRedirect(result.Error!.Code == GoogleCallbackCommandHandler.AccountExists.Code ? "/sign-in?google=existing" : Failed());
        }

        var outcome = result.Value!;
        if (outcome.SignedIn is not null)
        {
            cookies.Write(Response, outcome.SignedIn);
            return LocalRedirect(flow.ReturnUrl);
        }

        Response.Cookies.Append(JoinCookie, outcome.JoinToken!, JoinCookieOptions(clock.GetUtcNow().UtcDateTime + PendingExternalJoin.Lifetime));
        return LocalRedirect("/join/google?returnUrl=" + Uri.EscapeDataString(flow.ReturnUrl));
    }

    [HttpGet("pending")]
    public async Task<IActionResult> Pending(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPendingGoogleJoinQuery(Request.Cookies[JoinCookie]), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPost("finish")]
    public async Task<IActionResult> Finish(FinishGoogleJoinRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new FinishGoogleJoinCommand(Request.Cookies[JoinCookie], request.DisplayName, request.AgeConfirmed, request.TermsAccepted),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error!);
        }

        Response.Cookies.Delete(JoinCookie, JoinCookieOptions(null));
        cookies.Write(Response, result.Value!);
        var me = await sender.Send(new GetCurrentMemberQuery(result.Value!.MemberId), cancellationToken);
        return me.IsSuccess ? Ok(me.Value) : this.ToProblem(me.Error!);
    }

    /// <summary>Only a same-site path is followed after signing in; anything else lands on / (L2-088 AC10).</summary>
    public static string SafeReturnUrl(string? returnUrl) =>
        returnUrl is not null && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//", StringComparison.Ordinal) && !returnUrl.StartsWith("/\\", StringComparison.Ordinal)
            ? returnUrl
            : "/";

    private string RedirectUri()
    {
        if (!string.IsNullOrEmpty(options.Value.RedirectUri))
        {
            return options.Value.RedirectUri;
        }

        var scheme = Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
        return $"{(string.IsNullOrEmpty(scheme) ? Request.Scheme : scheme)}://{Request.Host}{CallbackPath}";
    }

    private string Failed() => "/sign-in?google=failed&ref=" + Uri.EscapeDataString(ProblemResults.CorrelationId(HttpContext));

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));

    private static CookieOptions JoinCookieOptions(DateTime? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = GoogleFlowCookie.CookiePath,
        Expires = expires,
        IsEssential = true,
    };
}

public sealed record FinishGoogleJoinRequest(string? DisplayName, bool AgeConfirmed, bool TermsAccepted);
