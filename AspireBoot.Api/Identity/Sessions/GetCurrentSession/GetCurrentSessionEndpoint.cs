using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity;
using AspireBoot.Core.Identity.Sessions.GetCurrentSession;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Sessions.GetCurrentSession;

public static class GetCurrentSessionEndpoint
{
    public static void MapGetCurrentSession(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapGet("/current", HandleAsync)
        .Produces<GetCurrentSessionResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .RequireRateLimiting(ApiConstants.RateLimiterAuthPolicyName)
        .WithName("Identity.Sessions.GetCurrentSession");

    private static async Task<IResult> HandleAsync(
        [FromServices] IQueryHandler<GetCurrentSessionQuery, GetCurrentSessionResult> queryHandler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Result<GetCurrentSessionResult> result = await queryHandler
            .HandleAsync(
                new GetCurrentSessionQuery(
                    httpContext.User.GetIdentityUserId(),
                    httpContext.User.GetEmail(),
                    httpContext.User.IsInRole(IdentityConstants.AdministratorRole)),
                cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? TypedResults.Ok(new GetCurrentSessionResponse(
                result.Value!.IdentityUserId, result.Value.UserId, result.Value.Email, result.Value.IsAdministrator))
            : result.ToProblemHttpResult(httpContext);
    }
}
