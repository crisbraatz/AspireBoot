using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Middlewares;

internal sealed class ExceptionMiddleware(
    RequestDelegate next,
    IProblemDetailsService problemDetailsService,
    ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled exception occurred");

            context.Response.StatusCode = e switch
            {
                ApplicationException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                Exception = e,
                ProblemDetails = new ProblemDetails
                {
                    Type = e.GetType().Name,
                    Title = "An error occured",
                    Detail = e.Message
                }
            });
        }
    }
}