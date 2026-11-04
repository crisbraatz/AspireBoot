using System.Security.Claims;
using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Auth.ChangeEmail;

public record ChangeEmailCommand(
    string NewEmail,
    string Password,
    ClaimsPrincipal ClaimsPrincipal,
    string FrontendOrigin)
    : ICommand;
