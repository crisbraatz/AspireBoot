using System.ComponentModel.DataAnnotations;
using AspireBoot.Core.Identity.Auth.ConfirmEmailChange;

namespace AspireBoot.Api.Identity.Auth.ConfirmEmailChange;

public record ConfirmEmailChangeRequest(
    [property: Required, EmailAddress, MaxLength(254)]
    string Email,
    [property: Required, MaxLength(255)] string IdentityUserId,
    [property: Required, MaxLength(4096)] string Token)
{
    public ConfirmEmailChangeCommand ToCommand() => new(Email, IdentityUserId, Token);
}
