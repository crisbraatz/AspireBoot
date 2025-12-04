using AspireBoot.Domain.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.ApiService.Middlewares;

public sealed class ExceptionMiddleware(
    RequestDelegate requestDelegate,
    IProblemDetailsService problemDetailsService,
    ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        using (logger.BeginScope(nameof(ExceptionMiddleware)))
        {
            try
            {
                await requestDelegate(httpContext).ConfigureAwait(false);
            }
#pragma warning disable CA1031
            catch (Exception exception)
            {
                LoggerMessageExtension.LogExceptionMiddlewareError(logger, exception);

                httpContext.Response.StatusCode = exception switch
                {
                    ApplicationException => StatusCodes.Status400BadRequest,
                    _ => StatusCodes.Status500InternalServerError
                };

                await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    Exception = exception,
                    ProblemDetails = new ProblemDetails
                    {
                        Type = exception.GetType().Name,
                        Title = "Unexpected exception occurred",
                        Detail = exception.Message
                    }
                }).ConfigureAwait(false);
            }
#pragma warning restore CA1031
        }
    }
}
