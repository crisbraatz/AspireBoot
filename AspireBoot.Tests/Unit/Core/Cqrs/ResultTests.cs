using AspireBoot.Core.Cqrs;

namespace AspireBoot.Tests.Unit.Core.Cqrs;

public class ResultTests
{
    [Fact]
    public void FailureShouldReturnFailureWhenPreservingAllFailuresInTheirOriginalOrder()
    {
        IReadOnlyList<Failure> failures = [Failure.Validation("email.invalid", "Invalid email.", "email"), Failure.Conflict("email.exists", "Email already exists.")];

        Result result = Result.Failure(failures);

        Assert.False(result.IsSuccess);
        Assert.Same(failures, result.Failures);
        Assert.NotNull(result.Failures);
        Assert.Equal("email.invalid", result.Failures[0].Code);
        Assert.Equal("email.exists", result.Failures[1].Code);
    }

    [Fact]
    public void FailureShouldReturnFailureWhenPreservingFailuresAndHaveNoValueForATypedFailure()
    {
        IReadOnlyList<Failure> failures = [Failure.NotFound("user.missing", "User not found.")];

        Result<object> result = Result<object>.Failure(failures);

        Assert.False(result.IsSuccess);
        Assert.Same(failures, result.Failures);
        Assert.Null(result.Value);
    }

    [Fact]
    public void FailureShouldReturnFailureWhenViewedAsTheBaseResult()
    {
        IReadOnlyList<Failure> failures = [Failure.ServiceUnavailable("storage.unavailable", "Unavailable.")];

        Result result = Result<string>.Failure(failures);

        Assert.False(result.IsSuccess);
        Assert.Same(failures, result.Failures);
    }

    [Fact]
    public void SuccessShouldReturnSuccessWithoutFailureDetails()
    {
        Result result = Result.Success();

        bool isSuccess = result.IsSuccess;

        Assert.True(isSuccess);
        Assert.Null(result.Failures);
    }

    [Fact]
    public void SuccessShouldReturnSuccessWithTheProvidedValue()
    {
        object expectedValue = new();

        Result<object> result = Result<object>.Success(expectedValue);

        Assert.True(result.IsSuccess);
        Assert.Same(expectedValue, result.Value);
        Assert.Null(result.Failures);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("value")]
    public void SuccessShouldReturnSuccessWhenTheValueIsNullOrEmpty(string? value = null)
    {
        Result<string?> result = Result<string?>.Success(value);

        bool isSuccess = result.IsSuccess;

        Assert.True(isSuccess);
        Assert.Equal(value, result.Value);
        Assert.Null(result.Failures);
    }
}
