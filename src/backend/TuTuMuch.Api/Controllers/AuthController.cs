using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Api.Auth;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Accounts;

namespace TuTuMuch.Api.Controllers;

/// <summary>Registration, email verification, sign-in and sessions (L2-001, L2-002, L2-006, L2-008).</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender, SessionCookies cookies, IGoogleIdentityProvider google) : ControllerBase
{
    /// <summary>Which third-party sign-ins are configured, so the pages know whether to offer them (L2-088).</summary>
    [HttpGet("providers")]
    public IActionResult Providers() => Ok(new SignInProvidersDto(google.ClientId is not null));

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RegisterMemberCommand(request.DisplayName, request.Email, request.Password, request.AgeConfirmed, request.TermsAccepted),
            cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created) : this.ToProblem(result.Error!);
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new VerifyEmailCommand(request.Token), cancellationToken);
        return result.IsSuccess ? Ok() : this.ToProblem(result.Error!);
    }

    [HttpPost("verification-email")]
    public async Task<IActionResult> RequestVerificationEmail(VerificationEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RequestVerificationEmailCommand(request.Email), cancellationToken);
        return result.IsSuccess ? Accepted() : this.ToProblem(result.Error!);
    }

    [HttpPost("sign-in")]
    public async Task<IActionResult> SignIn(SignInRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SignInCommand(request.Email, request.Password), cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error!);
        }

        cookies.Write(Response, result.Value!);
        return await Me(result.Value!.MemberId, cancellationToken);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshSessionCommand(Request.Cookies[SessionCookies.RefreshCookie]), cancellationToken);
        if (!result.IsSuccess)
        {
            SessionCookies.Clear(Response);
            return this.ToProblem(result.Error!);
        }

        cookies.Write(Response, result.Value!);
        return await Me(result.Value!.MemberId, cancellationToken);
    }

    [HttpPost("sign-out")]
    public async Task<IActionResult> SignOutMember(CancellationToken cancellationToken)
    {
        await sender.Send(new SignOutCommand(Request.Cookies[SessionCookies.RefreshCookie]), cancellationToken);
        SessionCookies.Clear(Response);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public Task<IActionResult> Me(CancellationToken cancellationToken) => Me(User.MemberId(), cancellationToken);

    private async Task<IActionResult> Me(Guid memberId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCurrentMemberQuery(memberId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}

public sealed record RegisterRequest(string? DisplayName, string? Email, string? Password, bool AgeConfirmed, bool TermsAccepted);

public sealed record VerifyEmailRequest(string? Token);

public sealed record VerificationEmailRequest(string? Email);

public sealed record SignInRequest(string? Email, string? Password);

public sealed record SignInProvidersDto(bool Google);
