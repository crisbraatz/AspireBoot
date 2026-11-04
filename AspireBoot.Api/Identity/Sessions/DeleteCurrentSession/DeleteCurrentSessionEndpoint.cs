using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions.EndSession;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Sessions.DeleteCurrentSession;

public static class DeleteCurrentSessionEndpoint
{
    public static void MapDeleteCurrentSession(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapDelete("/current", HandleAsync)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .RequireRateLimiting(ApiConstants.RateLimiterAuthPolicyName)
        .WithName("Identity.Sessions.DeleteCurrentSession");

    private static async Task<IResult> HandleAsync(
        [FromServices] ICommandHandler<EndSessionCommand> commandHandler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Result result =
            await commandHandler.HandleAsync(new EndSessionCommand(), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Results.NoContent() : result.ToProblemHttpResult(httpContext);
    }
}
