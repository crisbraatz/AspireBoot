using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ConfirmEmailChange;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Auth.ConfirmEmailChange;

public static class ConfirmEmailChangeEndpoint
{
    public static void MapConfirmEmailChange(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapPost("/confirm-email-change", HandleAsync)
        .Accepts<ConfirmEmailChangeRequest>("application/json")
        .AllowAnonymous()
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
        .RequireRateLimiting(ApiConstants.RateLimiterAuthPolicyName)
        .WithName("Identity.Auth.ConfirmEmailChange");

    private static async Task<IResult> HandleAsync(
        [FromBody] ConfirmEmailChangeRequest request,
        [FromServices] ICommandHandler<ConfirmEmailChangeCommand> commandHandler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Result result = await commandHandler.HandleAsync(request.ToCommand(), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemHttpResult(httpContext);
    }
}
