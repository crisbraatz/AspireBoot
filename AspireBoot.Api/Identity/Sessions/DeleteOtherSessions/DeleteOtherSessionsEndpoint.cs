using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions.DeleteOtherSessions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Sessions.DeleteOtherSessions;

public static class DeleteOtherSessionsEndpoint
{
    public static void MapDeleteOtherSessions(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapDelete("/others", HandleAsync)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .RequireRateLimiting(ApiConstants.RateLimiterAuthPolicyName)
        .WithName("Identity.Sessions.DeleteOtherSessions");

    private static async Task<IResult> HandleAsync(
        [FromServices] ICommandHandler<DeleteOtherSessionsCommand> commandHandler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        string? currentSessionId =
            (await httpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme).ConfigureAwait(false))
            .Properties?.Items[Core.Identity.IdentityConstants.SessionIdProperty];

        if (currentSessionId is null)
            return Result.Failure(
                [
                    Failure.Unauthorized(
                        "identity.auth.invalid-credentials",
                        "Invalid credentials or account unavailable.")
                ])
                .ToProblemHttpResult(httpContext);

        Result result = await commandHandler
            .HandleAsync(
                new DeleteOtherSessionsCommand(httpContext.User.GetIdentityUserId(), currentSessionId),
                cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemHttpResult(httpContext);
    }
}
