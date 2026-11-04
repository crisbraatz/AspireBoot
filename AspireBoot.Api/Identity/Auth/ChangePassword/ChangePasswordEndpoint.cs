using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ChangePassword;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Auth.ChangePassword;

public static class ChangePasswordEndpoint
{
    public static void MapChangePassword(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapPatch("/password", HandleAsync)
        .Accepts<ChangePasswordRequest>("application/json")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
        .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .ProducesValidationProblem()
        .RequireRateLimiting(ApiConstants.RateLimiterAuthPolicyName)
        .WithName("Identity.Auth.ChangePassword");

    private static async Task<IResult> HandleAsync(
        [FromBody] ChangePasswordRequest request,
        [FromServices] ICommandHandler<ChangePasswordCommand> commandHandler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Result result = await commandHandler
            .HandleAsync(request.ToCommand(httpContext.User), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemHttpResult(httpContext);
    }
}
