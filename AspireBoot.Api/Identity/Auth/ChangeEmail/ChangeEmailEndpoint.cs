using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ChangeEmail;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Auth.ChangeEmail;

public static class ChangeEmailEndpoint
{
    public static void MapChangeEmail(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapPatch("/email", HandleAsync)
        .Accepts<ChangeEmailRequest>("application/json")
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
        .WithName("Identity.Auth.ChangeEmail");

    private static async Task<IResult> HandleAsync(
        [FromBody] ChangeEmailRequest request,
        [FromServices] ICommandHandler<ChangeEmailCommand> commandHandler,
        [FromServices] FrontendOptions frontendOptions,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Result result = await commandHandler
            .HandleAsync(request.ToCommand(httpContext.User, frontendOptions.Origin), cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemHttpResult(httpContext);
    }
}
