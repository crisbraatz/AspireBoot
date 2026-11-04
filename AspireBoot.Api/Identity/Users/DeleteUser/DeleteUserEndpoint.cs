using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.DeleteUser;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Users.DeleteUser;

public static class DeleteUserEndpoint
{
    public static void MapDeleteUser(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapDelete("/me", HandleAsync)
        .Accepts<DeleteUserRequest>("application/json")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
        .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .ProducesValidationProblem()
        .RequireRateLimiting(ApiConstants.RateLimiterAuthPolicyName)
        .WithMetadata(new RequestSizeLimitAttribute(32 * 1024))
        .WithName("Identity.Users.DeleteUser");

    private static async Task<IResult> HandleAsync(
        [FromBody] DeleteUserRequest request,
        [FromServices] ICommandHandler<DeleteUserCommand> commandHandler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Result result = await commandHandler
            .HandleAsync(
                new DeleteUserCommand(
                    httpContext.User.GetUserId(), httpContext.User.GetIdentityUserId(), request.Password),
                cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemHttpResult(httpContext);
    }
}
