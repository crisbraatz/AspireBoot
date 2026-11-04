using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.UpdateUser;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Users.UpdateUser;

public static class UpdateUserEndpoint
{
    public static void MapUpdateUser(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapPatch("/me", HandleAsync)
        .Accepts<UpdateUserRequest>("multipart/form-data")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
        .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .ProducesValidationProblem()
        .RequireRateLimiting(ApiConstants.RateLimiterWritesPolicyName)
        .WithMetadata(new RequestSizeLimitAttribute(AvatarPolicy.MaxSizeInBytes + 64 * 1024))
        .WithName("Identity.Users.UpdateUser");

    private static async Task<IResult> HandleAsync(
        [FromServices] ICommandHandler<UpdateUserCommand> commandHandler,
        HttpContext httpContext,
        [FromForm] UpdateUserRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        Result result = await commandHandler
            .HandleAsync(
                await (request ?? new UpdateUserRequest())
                    .ToCommandAsync(httpContext.User.GetIdentityUserId(), cancellationToken).ConfigureAwait(false),
                cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemHttpResult(httpContext);
    }
}
