namespace AspireBoot.Core.Cqrs;

public enum FailureKind
{
    Conflict,
    Forbidden,
    NotFound,
    ServiceUnavailable,
    TooManyRequests,
    Unauthorized,
    UnprocessableEntity,
    Validation
}
