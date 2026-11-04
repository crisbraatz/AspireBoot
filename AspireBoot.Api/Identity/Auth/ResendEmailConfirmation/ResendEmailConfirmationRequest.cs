using System.ComponentModel.DataAnnotations;
using AspireBoot.Core.Identity.Auth.ResendEmailConfirmation;

namespace AspireBoot.Api.Identity.Auth.ResendEmailConfirmation;

public record ResendEmailConfirmationRequest(
    [property: Required, EmailAddress, MaxLength(254)]
    string Email)
{
    public ResendEmailConfirmationCommand ToCommand(string frontendOrigin) => new(Email, frontendOrigin);
};
