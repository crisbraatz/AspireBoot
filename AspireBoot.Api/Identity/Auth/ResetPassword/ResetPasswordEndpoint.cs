using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ResetPassword;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Auth.ResetPassword;

public static class ResetPasswordEndpoint
{
    public static void MapResetPassword(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapPost("/reset-password", HandleAsync)
        .Accepts<ResetPasswordRequest>("application/json")
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
        .WithName("Identity.Auth.ResetPassword");

    private static async Task<IResult> HandleAsync(
        [FromBody] ResetPasswordRequest request,
        [FromServices] ICommandHandler<ResetPasswordCommand> commandHandler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Result result = await commandHandler.HandleAsync(request.ToCommand(), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemHttpResult(httpContext);
    }
}
