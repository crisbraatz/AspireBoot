using System.ComponentModel.DataAnnotations;
using AspireBoot.Core.Identity.Auth.RequestRecovery;

namespace AspireBoot.Api.Identity.Auth.RequestRecovery;

public record RequestRecoveryRequest(
    [property: Required, EmailAddress, MaxLength(254)]
    string Email)
{
    public RequestRecoveryCommand ToCommand(string frontendOrigin) => new(Email, frontendOrigin);
}
