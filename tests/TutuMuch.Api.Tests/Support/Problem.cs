using System.Net.Http.Json;
using System.Text.Json;

namespace TutuMuch.Api.Tests.Support;

/// <summary>Reads an RFC 9457 problem details body with the Tutu Much code and field errors.</summary>
public sealed record Problem(string? Code, string? Title, string? CorrelationId, Dictionary<string, FieldProblem>? Errors)
{
    public static async Task<Problem> ReadAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<Problem>(new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;
}

public sealed record FieldProblem(string Code, string Message);
