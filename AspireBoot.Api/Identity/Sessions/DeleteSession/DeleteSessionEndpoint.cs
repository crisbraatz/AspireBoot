using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions.DeleteSession;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Sessions.DeleteSession;

public static class DeleteSessionEndpoint
{
    public static void MapDeleteSession(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapDelete("/{id}", HandleAsync)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .RequireRateLimiting(ApiConstants.RateLimiterAuthPolicyName)
        .WithName("Identity.Sessions.DeleteSession");

    private static async Task<IResult> HandleAsync(
        string id,
        [FromServices] ICommandHandler<DeleteSessionCommand> commandHandler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Result result = await commandHandler
            .HandleAsync(new DeleteSessionCommand(httpContext.User.GetIdentityUserId(), id), cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemHttpResult(httpContext);
    }
}
