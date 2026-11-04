using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Integrations.Smtp;

namespace AspireBoot.Tests.Unit.Core.Integrations.Smtp;

public class EmailFailuresTests
{
    [Fact]
    public void ShouldThrowInvalidOperationExceptionWhenFailureDetailsAreMissing()
    {
        Exception? exception = Record.Exception(() => EmailFailures.Throw());

        Assert.Equal("Failed email request has no failure details.", Assert.IsType<InvalidOperationException>(exception).Message);
    }

    [Fact]
    public void ShouldThrowEmailRequestRateLimitedExceptionWhenPrioritizeRateLimitingOverValidation()
    {
        IReadOnlyList<Failure> failures = [Failure.Validation("email.invalid", "Invalid.", "email"), Failure.TooManyRequests("email.limited", "Limited.")];

        Exception? exception = Record.Exception(() => EmailFailures.Throw(failures));

        Assert.IsType<EmailRequestRateLimitedException>(exception);
    }

    [Fact]
    public void ShouldThrowInvalidEmailRequestExceptionWhenClassifyValidationAsNonRetryable()
    {
        IReadOnlyList<Failure> failures = [Failure.Validation("email.invalid", "Invalid.", "email")];

        Exception? exception = Record.Exception(() => EmailFailures.Throw(failures));

        Assert.IsType<InvalidEmailRequestException>(exception);
    }

    [Fact]
    public void ShouldThrowInvalidOperationExceptionWhenLeaveDependencyFailuresRetryable()
    {
        IReadOnlyList<Failure> failures = [Failure.ServiceUnavailable("smtp.unavailable", "Unavailable.")];

        Exception? exception = Record.Exception(() => EmailFailures.Throw(failures));

        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void ShouldThrowInvalidEmailRequestExceptionWhenClassifyAnEmptyFailureListAsInvalid()
    {
        IReadOnlyList<Failure> failures = [];

        Exception? exception = Record.Exception(() => EmailFailures.Throw(failures));

        Assert.IsType<InvalidEmailRequestException>(exception);
    }

    [Fact]
    public void ShouldThrowInvalidOperationExceptionWhenNotClassifyMixedValidationAndDependencyFailuresAsInvalid()
    {
        IReadOnlyList<Failure> failures = [Failure.Validation("email.invalid", "Invalid.", "email"), Failure.ServiceUnavailable("smtp.unavailable", "Unavailable.")];

        Exception? exception = Record.Exception(() => EmailFailures.Throw(failures));

        Assert.IsType<InvalidOperationException>(exception);
    }
}
