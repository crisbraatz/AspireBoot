using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.Register;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Auth.Register;

public static class RegisterEndpoint
{
    public static void MapRegister(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapPost("/register", HandleAsync)
        .Accepts<RegisterRequest>("application/json")
        .AllowAnonymous()
        .Produces<RegisterResponse>(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
        .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .ProducesValidationProblem()
        .RequireRateLimiting(ApiConstants.RateLimiterAuthPolicyName)
        .WithName("Identity.Auth.Register");

    private static async Task<IResult> HandleAsync(
        [FromBody] RegisterRequest request,
        [FromServices] ICommandHandler<RegisterCommand, RegisterResult> commandHandler,
        [FromServices] FrontendOptions frontendOptions,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Result<RegisterResult> result = await commandHandler
            .HandleAsync(request.ToCommand(frontendOptions.Origin), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Accepted(value: new RegisterResponse(result.Value!.RequiresEmailConfirmation))
            : result.ToProblemHttpResult(httpContext);
    }
}
