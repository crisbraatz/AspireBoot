using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ConfirmEmail;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Auth.ConfirmEmail;

public static class ConfirmEmailEndpoint
{
    public static void MapConfirmEmail(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapPost("/confirm-email", HandleAsync)
        .Accepts<ConfirmEmailRequest>("application/json")
        .AllowAnonymous()
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
        .WithName("Identity.Auth.ConfirmEmail");

    private static async Task<IResult> HandleAsync(
        [FromBody] ConfirmEmailRequest request,
        [FromServices] ICommandHandler<ConfirmEmailCommand> commandHandler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Result result = await commandHandler.HandleAsync(request.ToCommand(), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemHttpResult(httpContext);
    }
}
