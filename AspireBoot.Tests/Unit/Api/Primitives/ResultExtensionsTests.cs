using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Tests.Unit.Api.Primitives;

public class ResultExtensionsTests
{
    [Fact]
    public void ShouldReturnValidationProblemDetailsGroupedByField()
    {
        Result result = Result.Failure([Failure.Validation("name.length", "Name is too short.", "newName"), Failure.Validation("name.characters", "Invalid characters.", "newName"), Failure.Validation("email.invalid", "Invalid email.", "email")]);
        DefaultHttpContext defaultHttpContext = new() { Request = { Path = "/api/users/me" } };

        ProblemHttpResult problemHttpResult = result.ToProblemHttpResult(defaultHttpContext);

        ValidationProblemDetails validationProblemDetails = Assert.IsType<ValidationProblemDetails>(problemHttpResult.ProblemDetails);
        Assert.Equal(400, problemHttpResult.StatusCode);
        Assert.Equal("urn:aspireboot:validation", validationProblemDetails.Type);
        Assert.Equal("/api/users/me", validationProblemDetails.Instance);
        Assert.Collection(validationProblemDetails.Errors["newName"], message => Assert.Equal("Name is too short.", message), message => Assert.Equal("Invalid characters.", message));
        Assert.Equal("Invalid email.", Assert.Single(validationProblemDetails.Errors["email"]));
        IReadOnlyDictionary<string, string[]> codes = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string[]>>(validationProblemDetails.Extensions["codes"]);
        Assert.Collection(codes["newName"], code => Assert.Equal("name.length", code), code => Assert.Equal("name.characters", code));
        Assert.Equal("email.invalid", Assert.Single(codes["email"]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ShouldReturnValidationProblemDetailsWithFieldlessFailuresUnderAnEmptyKey(string? field = null)
    {
        ProblemHttpResult problemHttpResult = Result.Failure([new Failure(FailureKind.Validation, "request.invalid", "Invalid request.", field)]).ToProblemHttpResult(new DefaultHttpContext());

        ValidationProblemDetails validationProblemDetails = Assert.IsType<ValidationProblemDetails>(problemHttpResult.ProblemDetails);
        Assert.Equal("", Assert.Single(validationProblemDetails.Errors).Key);
        Assert.Equal("request.invalid", Assert.Single(Assert.IsAssignableFrom<IReadOnlyDictionary<string, string[]>>(validationProblemDetails.Extensions["codes"])[""]));
    }

    [Theory]
    [InlineData(FailureKind.Unauthorized, 401, "Unauthorized")]
    [InlineData(FailureKind.Forbidden, 403, "Forbidden")]
    [InlineData(FailureKind.NotFound, 404, "Not found")]
    [InlineData(FailureKind.Conflict, 409, "Conflict")]
    [InlineData(FailureKind.UnprocessableEntity, 422, "Unprocessable entity")]
    [InlineData(FailureKind.TooManyRequests, 429, "Too many requests")]
    [InlineData(FailureKind.ServiceUnavailable, 503, "Service unavailable")]
    [InlineData((FailureKind)999, 500, "Internal server error")]
    public void ShouldReturnProblemDetailsMatchingTheDomainFailure(FailureKind failureKind, int statusCode, string title)
    {
        Failure failure = new(failureKind, "operation.failed", "Operation failed.");
        Result result = Result.Failure([failure]);
        DefaultHttpContext defaultHttpContext = new() { Request = { Path = "/api/operation" } };

        ProblemHttpResult problemHttpResult = result.ToProblemHttpResult(defaultHttpContext);

        Assert.Equal(statusCode, problemHttpResult.StatusCode);
        Assert.Equal(title, problemHttpResult.ProblemDetails.Title);
        Assert.Equal(failure.Message, problemHttpResult.ProblemDetails.Detail);
        Assert.Equal(failure.Code, problemHttpResult.ProblemDetails.Extensions["code"]);
        Assert.Equal("urn:aspireboot:operation.failed", problemHttpResult.ProblemDetails.Type);
        Assert.Equal("/api/operation", problemHttpResult.ProblemDetails.Instance);
    }

    [Fact]
    public void ShouldReturnProblemDetailsForTheFirstFailureWhenFailureKindsAreMixed()
    {
        ProblemHttpResult problemHttpResult = Result.Failure([Failure.NotFound("user.missing", "User not found."), Failure.Validation("name.invalid", "Invalid name.", "name")]).ToProblemHttpResult(new DefaultHttpContext());

        Assert.Equal(404, problemHttpResult.StatusCode);
        Assert.IsNotType<ValidationProblemDetails>(problemHttpResult.ProblemDetails);
        Assert.Equal("user.missing", problemHttpResult.ProblemDetails.Extensions["code"]);
    }

    [Fact]
    public void ShouldThrowInvalidOperationExceptionWhenTheResultIsSuccessful()
    {
        Result result = Result.Success();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => result.ToProblemHttpResult(new DefaultHttpContext()));

        Assert.Equal("Only a failed result can be converted to ProblemHttpResult.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldThrowInvalidOperationExceptionWhenFailureDetailsAreMissing(bool missing)
    {
        Result result = Result.Failure(missing ? null! : Array.Empty<Failure>());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => result.ToProblemHttpResult(new DefaultHttpContext()));

        Assert.Equal("Only a failed result can be converted to ProblemHttpResult.", exception.Message);
    }
}
