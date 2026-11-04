using System.ComponentModel.DataAnnotations;
using AspireBoot.Core.Identity.Auth.ResetPassword;

namespace AspireBoot.Api.Identity.Auth.ResetPassword;

public record ResetPasswordRequest(
    [property: Required, EmailAddress, MaxLength(254)]
    string Email,
    [property: Required, StringLength(128, MinimumLength = 12)]
    string Password,
    [property: Required, MaxLength(4096)] string Token)
{
    public ResetPasswordCommand ToCommand() => new(Email, Password, Token);
};
