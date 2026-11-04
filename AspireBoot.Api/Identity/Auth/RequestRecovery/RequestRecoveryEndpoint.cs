using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.RequestRecovery;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Auth.RequestRecovery;

public static class RequestRecoveryEndpoint
{
    public static void MapRequestRecovery(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapPost("/forgot-password", HandleAsync)
        .Accepts<RequestRecoveryRequest>("application/json")
        .AllowAnonymous()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
        .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .ProducesValidationProblem()
        .RequireRateLimiting(ApiConstants.RateLimiterAuthPolicyName)
        .WithName("Identity.Auth.RequestRecovery");

    private static async Task<IResult> HandleAsync(
        [FromBody] RequestRecoveryRequest request,
        [FromServices] ICommandHandler<RequestRecoveryCommand> commandHandler,
        [FromServices] FrontendOptions frontendOptions,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Result result = await commandHandler
            .HandleAsync(request.ToCommand(frontendOptions.Origin), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemHttpResult(httpContext);
    }
}
