using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ResendEmailConfirmation;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Auth.ResendEmailConfirmation;

public static class ResendEmailConfirmationEndpoint
{
    public static void MapResendEmailConfirmation(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapPost("/resend-email-confirmation", HandleAsync)
        .Accepts<ResendEmailConfirmationRequest>("application/json")
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
        .WithName("Identity.Auth.ResendEmailConfirmation");

    private static async Task<IResult> HandleAsync(
        [FromBody] ResendEmailConfirmationRequest request,
        [FromServices] ICommandHandler<ResendEmailConfirmationCommand> commandHandler,
        [FromServices] FrontendOptions frontendOptions,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Result result = await commandHandler
            .HandleAsync(request.ToCommand(frontendOptions.Origin), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemHttpResult(httpContext);
    }
}
