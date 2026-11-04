using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace AspireBoot.Api.Primitives;

public static class ClaimsPrincipalExtensions
{
    public static string GetEmail(this ClaimsPrincipal claimsPrincipal) =>
        claimsPrincipal.FindFirstValue("email") ?? throw new InvalidOperationException("Unable to get email.");

    public static string GetIdentityUserId(this ClaimsPrincipal claimsPrincipal) =>
        claimsPrincipal.FindFirstValue("sub") ?? throw new InvalidOperationException("Unable to get identity user ID.");

    public static Guid GetUserId(this ClaimsPrincipal claimsPrincipal) =>
        Guid.TryParse(claimsPrincipal.FindFirstValue(ApiConstants.UserIdClaimType), out Guid userId)
            ? userId
            : throw new InvalidOperationException("Unable to get user ID.");

    public static async Task<bool> IsAdministratorAsync(
        this ClaimsPrincipal claimsPrincipal, IAuthorizationService authorizationService) =>
        (await authorizationService
            .AuthorizeAsync(claimsPrincipal, ApiConstants.AuthorizationPolicyName).ConfigureAwait(false)).Succeeded;
}
