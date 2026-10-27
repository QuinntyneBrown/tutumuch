using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Common;

namespace TuTuMuch.Api.Auth;

/// <summary>Policy names of L2-010.</summary>
public static class Policies
{
    /// <summary>A signed-in member whose email is verified (L2-002 AC4, L2-003).</summary>
    public const string VerifiedMember = "VerifiedMember";

    /// <summary>A verified member may sell; there is no seller application (L2-003).</summary>
    public const string CanSell = "CanSell";

    /// <summary>A member holding the Administrator role (L2-010 AC3); only order problems use it (L2-036).</summary>
    public const string Administrator = "Administrator";
}

/// <summary>Requires a verified email, read from TuTuMuch database so verification takes effect at once.</summary>
public sealed class VerifiedEmailRequirement : IAuthorizationRequirement;

public sealed class VerifiedEmailHandler(ITuTuMuchDbContext db) : AuthorizationHandler<VerifiedEmailRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, VerifiedEmailRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var memberId = context.User.MemberId();
        if (await db.Members.AnyAsync(m => m.Id == memberId && m.EmailVerifiedAt != null))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail(new AuthorizationFailureReason(this, "EMAIL_NOT_VERIFIED"));
        }
    }
}

/// <summary>Requires the Administrator role, read from TuTuMuch database so a granted or removed role takes effect at once.</summary>
public sealed class AdministratorRequirement : IAuthorizationRequirement;

public sealed class AdministratorHandler(ITuTuMuchDbContext db) : AuthorizationHandler<AdministratorRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdministratorRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var memberId = context.User.MemberId();
        if (await db.Members.AnyAsync(m => m.Id == memberId && m.IsAdministrator))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail(new AuthorizationFailureReason(this, "ADMINISTRATOR_REQUIRED"));
        }
    }
}

/// <summary>Answers 401 and 403 as problem details; 403 for an unverified member carries EMAIL_NOT_VERIFIED.</summary>
public sealed class ProblemAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult result)
    {
        if (result.Challenged)
        {
            return ProblemResults.WriteAsync(context, new Error(401, "UNAUTHENTICATED", "Sign in to continue"));
        }

        if (result.Forbidden)
        {
            var reasons = result.AuthorizationFailure?.FailureReasons.Select(r => r.Message).ToList() ?? [];
            return ProblemResults.WriteAsync(context, reasons.Contains("EMAIL_NOT_VERIFIED")
                ? new Error(403, "EMAIL_NOT_VERIFIED", "Verify your email to sell")
                : reasons.Contains("ADMINISTRATOR_REQUIRED")
                    ? new Error(403, "ADMINISTRATOR_REQUIRED", "Only TuTuMuch administrators can do that")
                    : new Error(403, "FORBIDDEN", "You can't do that"));
        }

        return _default.HandleAsync(next, context, policy, result);
    }
}

public static class AuthSetup
{
    public static IServiceCollection AddTuTuMuchAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection("Auth"));
        services.AddSingleton<SessionCookies>();
        services.AddOptions<GoogleOptions>()
            .Bind(configuration.GetSection(GoogleOptions.Section))
            .Validate(o => o.IsConsistent, "Auth:Google needs both ClientId and ClientSecret, or neither")
            .ValidateOnStart();
        services.AddHttpClient(nameof(GoogleIdentityProvider));
        services.AddSingleton<IGoogleIdentityProvider, GoogleIdentityProvider>();
        services.AddSingleton<GoogleFlowCookie>();
        services.AddScoped<IAuthorizationHandler, VerifiedEmailHandler>();
        services.AddScoped<IAuthorizationHandler, AdministratorHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemAuthorizationResultHandler>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthOptions>, TimeProvider>((jwt, auth, clock) =>
            {
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = auth.Value.Issuer,
                    ValidAudience = auth.Value.Issuer,
                    IssuerSigningKey = auth.Value.Key,
                    ClockSkew = TimeSpan.Zero,
                    LifetimeValidator = (notBefore, expires, _, _) =>
                    {
                        var now = clock.GetUtcNow().UtcDateTime;
                        return (notBefore is null || notBefore <= now) && expires > now;
                    },
                };
                jwt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies[SessionCookies.AccessCookie];
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.VerifiedMember, p => p.RequireAuthenticatedUser().AddRequirements(new VerifiedEmailRequirement()))
            .AddPolicy(Policies.CanSell, p => p.RequireAuthenticatedUser().AddRequirements(new VerifiedEmailRequirement()))
            .AddPolicy(Policies.Administrator, p => p.RequireAuthenticatedUser().AddRequirements(new AdministratorRequirement()));
        return services;
    }
}
