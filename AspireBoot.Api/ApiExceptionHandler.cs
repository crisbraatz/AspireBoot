using AspireBoot.Infrastructure.Observability;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api;

public class ApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is IOException or OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;

            return true;
        }

        bool isUnavailable = DependencyFailures.IsUnavailable(exception);

        string detail = exception switch
        {
            BadHttpRequestException =>
                "The request could not be read. Check its content type, format and size and try again.",
            _ when isUnavailable => "A required service is temporarily unavailable. Try again later.",
            _ => "An unexpected error occurred."
        };

        int status = exception switch
        {
            BadHttpRequestException badHttpRequestException => badHttpRequestException.StatusCode,
            _ when isUnavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };

        httpContext.Response.StatusCode = status;

        return await problemDetailsService
            .TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Detail = detail,
                    Instance = httpContext.Request.Path.Value,
                    Status = status,
                    Title = status switch
                    {
                        StatusCodes.Status413PayloadTooLarge => "Payload too large",
                        StatusCodes.Status415UnsupportedMediaType => "Unsupported media type",
                        StatusCodes.Status500InternalServerError => "Internal server error",
                        StatusCodes.Status503ServiceUnavailable => "Service unavailable",
                        _ => "Invalid request"
                    }
                }
            })
            .ConfigureAwait(false);
    }
}
