using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.ListUserSessions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Users.ListUserSessions;

public static class ListUserSessionsEndpoint
{
    public static void MapListUserSessions(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapGet("/{id:guid}/sessions", HandleAsync)
        .Produces<ListUserSessionsResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .ProducesValidationProblem()
        .RequireRateLimiting(ApiConstants.RateLimiterAuthPolicyName)
        .WithName("Identity.Users.ListUserSessions");

    private static async Task<IResult> HandleAsync(
        Guid id,
        [AsParameters] ListRequest request,
        [FromServices] IQueryHandler<ListUserSessionsQuery, ListUserSessionsResult> queryHandler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        string? currentSessionId =
            (await httpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme).ConfigureAwait(false))
            .Properties?.Items[Core.Identity.IdentityConstants.SessionIdProperty];

        if (currentSessionId is null)
            return Result.Failure(
                [
                    Failure.Unauthorized(
                        "identity.auth.invalid-credentials",
                        "Invalid credentials.")
                ])
                .ToProblemHttpResult(httpContext);

        Result<ListUserSessionsResult> result = await queryHandler
            .HandleAsync(
                new ListUserSessionsQuery(
                    id, httpContext.User.GetUserId(), currentSessionId, request.Page, request.Size),
                cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? TypedResults.Ok(new ListUserSessionsResponse(
                result.Value!.Items
                    .Select(userSession =>
                        new ListUserSessionItemResponse(
                            userSession.Id,
                            userSession.CreatedAtUtc,
                            userSession.ExpiresAtUtc,
                            userSession.TrackedDetails?.Browser!,
                            userSession.TrackedDetails?.OperatingSystem!,
                            userSession.TrackedDetails?.DeviceType!,
                            userSession.TrackedDetails?.RemoteIpAddress,
                            userSession.IsCurrent))
                    .ToArray(),
                result.Value.Page,
                result.Value.Size,
                result.Value.Count))
            : result.ToProblemHttpResult(httpContext);
    }
}
