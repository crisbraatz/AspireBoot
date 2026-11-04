using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity;
using AspireBoot.Core.Identity.Users.ListUsers;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Users.ListUsers;

public static class ListUsersEndpoint
{
    public static void MapListUsers(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapGet("", HandleAsync)
        .Produces<ListUsersResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .ProducesValidationProblem()
        .RequireRateLimiting(ApiConstants.RateLimiterReadsPolicyName)
        .WithName("Identity.Users.ListUsers");

    private static async Task<IResult> HandleAsync(
        [AsParameters] ListRequest request,
        [FromServices] IQueryHandler<ListUsersQuery, ListUsersResult> queryHandler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Result<ListUsersResult> result = await queryHandler
            .HandleAsync(
                new ListUsersQuery(
                    request.Page, request.Size, httpContext.User.IsInRole(IdentityConstants.AdministratorRole)),
                cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? TypedResults.Ok(new ListUsersResponse(
                result.Value!.Items
                    .Select(user =>
                        new ListUserItemResponse(
                            user.Id, user.Name, user.IsActive, user.IsAdministrator, user.AvatarUrl))
                    .ToArray(),
                result.Value.Page,
                result.Value.Size,
                result.Value.Count))
            : result.ToProblemHttpResult(httpContext);
    }
}
