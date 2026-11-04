using AspireBoot.Core.Cqrs;

namespace AspireBoot.Tests.Unit.Core.Cqrs;

public class FailureTests
{
    [Theory]
    [InlineData(FailureKind.Conflict)]
    [InlineData(FailureKind.Forbidden)]
    [InlineData(FailureKind.NotFound)]
    [InlineData(FailureKind.ServiceUnavailable)]
    [InlineData(FailureKind.Unauthorized)]
    [InlineData(FailureKind.UnprocessableEntity)]
    [InlineData(FailureKind.Validation)]
    public void FactoriesShouldReturnFailureWhenPreservingTheKindCodeMessageAndField(FailureKind failureKind)
    {
        const string code = "operation.failed";
        const string message = "Operation failed.";
        const string field = "email";

        Failure failure = CreateFailure(failureKind, code, message, field);

        Assert.Equal(failureKind, failure.FailureKind);
        Assert.Equal(code, failure.Code);
        Assert.Equal(message, failure.Message);
        Assert.Equal(field, failure.Field);
    }

    [Theory]
    [InlineData(FailureKind.Conflict)]
    [InlineData(FailureKind.Forbidden)]
    [InlineData(FailureKind.NotFound)]
    [InlineData(FailureKind.ServiceUnavailable)]
    [InlineData(FailureKind.Unauthorized)]
    [InlineData(FailureKind.UnprocessableEntity)]
    public void FactoriesShouldReturnFailureWhenFailuresWithoutAnAssociatedFieldAreProvided(FailureKind failureKind)
    {
        const string code = "operation.failed";
        const string message = "Operation failed.";

        Failure failure = CreateFailure(failureKind, code, message);

        Assert.Null(failure.Field);
        Assert.Equal(code, failure.Code);
        Assert.Equal(message, failure.Message);
    }

    [Fact]
    public void TooManyRequestsShouldReturnFailureWhenCreatingARateLimitFailureWithoutAField()
    {
        const string code = "email.rate-limit.exceeded";
        const string message = "Too many email requests.";

        Failure failure = Failure.TooManyRequests(code, message);

        Assert.Equal(FailureKind.TooManyRequests, failure.FailureKind);
        Assert.Equal(code, failure.Code);
        Assert.Equal(message, failure.Message);
        Assert.Null(failure.Field);
    }

    private static Failure CreateFailure(FailureKind failureKind, string code, string message, string? field = null) => failureKind switch { FailureKind.Conflict => Failure.Conflict(code, message, field), FailureKind.Forbidden => Failure.Forbidden(code, message, field), FailureKind.NotFound => Failure.NotFound(code, message, field), FailureKind.ServiceUnavailable => Failure.ServiceUnavailable(code, message, field), FailureKind.TooManyRequests => Failure.TooManyRequests(code, message), FailureKind.Unauthorized => Failure.Unauthorized(code, message, field), FailureKind.UnprocessableEntity => Failure.UnprocessableEntity(code, message, field), FailureKind.Validation => Failure.Validation(code, message, field!), _ => throw new ArgumentOutOfRangeException(nameof(failureKind)) };
}
