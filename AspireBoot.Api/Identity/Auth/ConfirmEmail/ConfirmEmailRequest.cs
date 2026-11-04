using System.ComponentModel.DataAnnotations;
using AspireBoot.Core.Identity.Auth.ConfirmEmail;

namespace AspireBoot.Api.Identity.Auth.ConfirmEmail;

public record ConfirmEmailRequest(
    [property: Required, MaxLength(255)] string IdentityUserId,
    [property: Required, MaxLength(4096)] string Token)
{
    public ConfirmEmailCommand ToCommand() => new(IdentityUserId, Token);
};
