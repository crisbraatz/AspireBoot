namespace AspireBoot.Core.Cqrs;

public record Failure(FailureKind FailureKind, string Code, string Message, string? Field = null)
{
    public static Failure Conflict(string code, string message, string? field = null) =>
        new(FailureKind.Conflict, code, message, field);

    public static Failure Forbidden(string code, string message, string? field = null) =>
        new(FailureKind.Forbidden, code, message, field);

    public static Failure NotFound(string code, string message, string? field = null) =>
        new(FailureKind.NotFound, code, message, field);

    public static Failure ServiceUnavailable(string code, string message, string? field = null) =>
        new(FailureKind.ServiceUnavailable, code, message, field);

    public static Failure TooManyRequests(string code, string message) =>
        new(FailureKind.TooManyRequests, code, message);

    public static Failure Unauthorized(string code, string message, string? field = null) =>
        new(FailureKind.Unauthorized, code, message, field);

    public static Failure UnprocessableEntity(string code, string message, string? field = null) =>
        new(FailureKind.UnprocessableEntity, code, message, field);

    public static Failure Validation(string code, string message, string field) =>
        new(FailureKind.Validation, code, message, field);
}
