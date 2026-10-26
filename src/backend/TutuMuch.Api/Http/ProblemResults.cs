using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using TutuMuch.Application.Common;

namespace TutuMuch.Api.Http;

/// <summary>Writes an application <see cref="Error"/> as RFC 9457 problem details with code and correlation ID (L2-068).</summary>
public static class ProblemResults
{
    public static IActionResult ToProblem(this ControllerBase controller, Error error)
    {
        var context = controller.HttpContext;
        if (error.RetryAfter is { } retryAfter)
        {
            context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        return new ObjectResult(Body(context, error)) { StatusCode = error.Status, ContentTypes = { "application/problem+json" } };
    }

    public static ProblemBody Body(HttpContext context, Error error) =>
        new(error.Title, error.Status, error.Code, CorrelationId(context), error.Errors);

    public static Task WriteAsync(HttpContext context, Error error)
    {
        context.Response.StatusCode = error.Status;
        return context.Response.WriteAsJsonAsync(Body(context, error), (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }

    private static string CorrelationId(HttpContext context) =>
        Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
}

public sealed record ProblemBody(string Title, int Status, string Code, string CorrelationId, IReadOnlyDictionary<string, FieldError>? Errors);
