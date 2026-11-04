using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AspireBoot.Core.Identity.Auth.ChangeEmail;

namespace AspireBoot.Api.Identity.Auth.ChangeEmail;

public record ChangeEmailRequest(
    [property: Required, EmailAddress, MaxLength(254)]
    string NewEmail,
    [property: Required, StringLength(128, MinimumLength = 12)]
    string Password)
{
    public ChangeEmailCommand ToCommand(ClaimsPrincipal claimsPrincipal, string frontendOrigin) =>
        new(NewEmail, Password, claimsPrincipal, frontendOrigin);
}
