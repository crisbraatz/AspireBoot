using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Integrations.Smtp;

public static class EmailFailures
{
    public static void Throw(IReadOnlyList<Failure>? failures = null)
    {
        if (failures is null)
            throw new InvalidOperationException("Failed email request has no failure details.");

        if (failures.Any(failure => failure.FailureKind is FailureKind.TooManyRequests))
            throw new EmailRequestRateLimitedException();

        if (failures.All(failure => failure.FailureKind is FailureKind.Validation))
            throw new InvalidEmailRequestException();

        throw new InvalidOperationException();
    }
}
