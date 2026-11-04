using System.ComponentModel.DataAnnotations;
using AspireBoot.Core.Identity.Auth.Register;
using AspireBoot.Core.Identity.Users;

namespace AspireBoot.Api.Identity.Auth.Register;

public record RegisterRequest(
    [property: Required, EmailAddress, MaxLength(254)]
    string Email,
    [property: Required, StringLength(128, MinimumLength = 12)]
    string Password,
    [property: Required, StringLength(100, MinimumLength = 2)]
    [property: RegularExpression(
        NamePolicy.AllowedPattern,
        ErrorMessage = "Name must contain only letters and spaces.",
        MatchTimeoutInMilliseconds = 100)]
    string Name)
{
    public RegisterCommand ToCommand(string frontendOrigin) => new(Email, Password, Name, frontendOrigin);
}
