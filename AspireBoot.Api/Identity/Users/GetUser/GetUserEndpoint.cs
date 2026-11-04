using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.GetUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Users.GetUser;

public static class GetUserEndpoint
{
    public static void MapGetUser(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapGet("/{id:guid}", HandleAsync)
        .Produces<GetUserResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .ProducesValidationProblem()
        .RequireRateLimiting(ApiConstants.RateLimiterReadsPolicyName)
        .WithName("Identity.Users.GetUser");

    private static async Task<IResult> HandleAsync(
        Guid id,
        [FromServices] IQueryHandler<GetUserQuery, GetUserResult> queryHandler,
        [FromServices] IAuthorizationService authorizationService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Result<GetUserResult> result = await queryHandler
            .HandleAsync(
                new GetUserQuery(
                    id,
                    httpContext.User.GetUserId(),
                    await httpContext.User.IsAdministratorAsync(authorizationService).ConfigureAwait(false)),
                cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? TypedResults.Ok(new GetUserResponse(
                result.Value!.Id,
                result.Value.Name,
                result.Value.IsActive,
                result.Value.IsAdministrator,
                result.Value.Email,
                result.Value.AvatarUrl))
            : result.ToProblemHttpResult(httpContext);
    }
}
