using AspireBoot.Core.Cqrs;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Primitives;

public static class ResultExtensions
{
    public static ProblemHttpResult ToProblemHttpResult(this Result result, HttpContext httpContext)
    {
        if (result.IsSuccess || result.Failures is not { Count: > 0 })
            throw new InvalidOperationException("Only a failed result can be converted to ProblemHttpResult.");

        return result.Failures.All(failure => failure.FailureKind is FailureKind.Validation)
            ? ToValidationProblemDetails(result.Failures, httpContext)
            : ToProblemDetails(result.Failures[0], httpContext);
    }

    private static ProblemHttpResult ToProblemDetails(Failure failure, HttpContext httpContext)
    {
        (int status, string title) = failure.FailureKind switch
        {
            FailureKind.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            FailureKind.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            FailureKind.NotFound => (StatusCodes.Status404NotFound, "Not found"),
            FailureKind.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            FailureKind.UnprocessableEntity => (StatusCodes.Status422UnprocessableEntity, "Unprocessable entity"),
            FailureKind.TooManyRequests => (StatusCodes.Status429TooManyRequests, "Too many requests"),
            FailureKind.ServiceUnavailable => (StatusCodes.Status503ServiceUnavailable, "Service unavailable"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        return TypedResults.Problem(new ProblemDetails
        {
            Detail = failure.Message,
            Extensions = { ["code"] = failure.Code },
            Instance = httpContext.Request.Path.Value,
            Status = status,
            Title = title,
            Type = $"urn:aspireboot:{failure.Code}"
        });
    }

    private static ProblemHttpResult ToValidationProblemDetails(
        IReadOnlyList<Failure> failures, HttpContext httpContext) =>
        TypedResults.Problem(new ValidationProblemDetails(failures
            .GroupBy(
                failure => !string.IsNullOrWhiteSpace(failure.Field) ? failure.Field : string.Empty,
                StringComparer.Ordinal)
            .ToDictionary(
                key => key.Key,
                value => value.Select(failure => failure.Message).ToArray(), StringComparer.Ordinal))
        {
            Detail = "One or more validation errors occurred.",
            Extensions =
            {
                ["codes"] = failures
                    .GroupBy(
                        failure => !string.IsNullOrWhiteSpace(failure.Field) ? failure.Field : string.Empty,
                        StringComparer.Ordinal)
                    .ToDictionary(
                        key => key.Key,
                        value => value.Select(failure => failure.Code).ToArray(), StringComparer.Ordinal)
            },
            Instance = httpContext.Request.Path.Value,
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation error",
            Type = "urn:aspireboot:validation"
        });
}
