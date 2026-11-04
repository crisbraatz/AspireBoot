using System.Security.Claims;
using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.GetCurrentUser;

namespace AspireBoot.Api.Identity.Users;

public class UsersEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        Result<GetCurrentUserResult> result = await context.HttpContext.RequestServices
            .GetRequiredService<IQueryHandler<GetCurrentUserQuery, GetCurrentUserResult>>()
            .HandleAsync(
                new GetCurrentUserQuery(context.HttpContext.User.GetIdentityUserId()),
                context.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
            return result.ToProblemHttpResult(context.HttpContext);

        if (context.HttpContext.User.Identity is not ClaimsIdentity claimsIdentity)
            return await next(context).ConfigureAwait(false);

        foreach (Claim claim in claimsIdentity.FindAll(ApiConstants.UserIdClaimType).ToArray())
            claimsIdentity.RemoveClaim(claim);

        claimsIdentity.AddClaim(new Claim(ApiConstants.UserIdClaimType, result.Value!.Id.ToString()));

        return await next(context).ConfigureAwait(false);
    }
}
