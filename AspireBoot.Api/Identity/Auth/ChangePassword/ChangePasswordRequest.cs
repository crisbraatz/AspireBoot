using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AspireBoot.Core.Identity.Auth.ChangePassword;

namespace AspireBoot.Api.Identity.Auth.ChangePassword;

public record ChangePasswordRequest(
    [property: Required, StringLength(128, MinimumLength = 12)]
    string CurrentPassword,
    [property: Required, StringLength(128, MinimumLength = 12)]
    string NewPassword)
{
    public ChangePasswordCommand ToCommand(ClaimsPrincipal claimsPrincipal) =>
        new(CurrentPassword, NewPassword, claimsPrincipal);
}
