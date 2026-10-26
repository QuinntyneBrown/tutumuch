namespace TuTuMuch.Application.Common;

/// <summary>
/// A failure the API turns into RFC 9457 problem details (L2-068): HTTP status, TuTuMuch code, title,
/// optional per-field errors for 422 and an optional Retry-After for 429.
/// </summary>
public sealed record Error(int Status, string Code, string Title, IReadOnlyDictionary<string, FieldError>? Errors = null)
{
    public TimeSpan? RetryAfter { get; init; }

    public static Error Invalid(string code, IReadOnlyDictionary<string, FieldError> errors, string title = "Check the highlighted fields") =>
        new(422, code, title, errors);

    public static Error NotFound(string code = "NOT_FOUND") => new(404, code, "Not found");

    public static Error TooManyRequests(TimeSpan retryAfter, string title = "Too many requests") =>
        new(429, "RATE_LIMITED", title) { RetryAfter = retryAfter };
}

/// <summary>One field's failure inside a 422: a stable code and the message the form shows.</summary>
public sealed record FieldError(string Code, string Message);

/// <summary>Either a value or an <see cref="Error"/>.</summary>
public readonly record struct Result<T>
{
    private Result(T? value, Error? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public static implicit operator Result<T>(T value) => new(value, null);

    public static implicit operator Result<T>(Error error) => new(default, error);
}
