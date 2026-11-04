using AspireBoot.Api.Primitives;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Identity.Auth.GetAntiforgery;

public static class GetAntiforgeryEndpoint
{
    public static void MapGetAntiforgery(this RouteGroupBuilder routeGroupBuilder) => routeGroupBuilder
        .MapGet("/csrf", Handle)
        .AllowAnonymous()
        .Produces<GetAntiforgeryResponse>()
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .RequireRateLimiting(ApiConstants.RateLimiterAuthPolicyName)
        .WithName("Identity.Auth.GetAntiforgery");

    private static Ok<GetAntiforgeryResponse> Handle([FromServices] IAntiforgery antiforgery, HttpContext httpContext)
        => TypedResults.Ok(new GetAntiforgeryResponse(antiforgery.GetAndStoreTokens(httpContext).RequestToken!));
}
