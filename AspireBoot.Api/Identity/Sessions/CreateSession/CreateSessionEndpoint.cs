using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions.CreateSession;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Sessions.CreateSession;

public static class CreateSessionEndpoint
{
    public static void MapCreateSession(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapPost("", HandleAsync)
        .Accepts<CreateSessionRequest>("application/json")
        .AllowAnonymous()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
        .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .ProducesValidationProblem()
        .RequireRateLimiting(ApiConstants.RateLimiterAuthPolicyName)
        .WithName("Identity.Sessions.CreateSession");

    private static async Task<IResult> HandleAsync(
        [FromBody] CreateSessionRequest request,
        [FromServices] ICommandHandler<CreateSessionCommand> commandHandler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (httpContext.User.Identity?.IsAuthenticated is true)
            return Result.Failure(
                [
                    Failure.Conflict(
                        "identity.sessions.already-authenticated",
                        "Sign out before starting another session in this browser.")
                ])
                .ToProblemHttpResult(httpContext);

        Result result = await commandHandler
            .HandleAsync(
                request.ToCommand(
                    httpContext.Request.Headers.UserAgent.ToString(),
                    httpContext.Connection.RemoteIpAddress?.ToString()),
                cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemHttpResult(httpContext);
    }
}
