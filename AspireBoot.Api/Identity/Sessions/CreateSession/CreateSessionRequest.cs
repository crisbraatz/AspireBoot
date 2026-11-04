using System.ComponentModel.DataAnnotations;
using AspireBoot.Core.Identity.Sessions.CreateSession;

namespace AspireBoot.Api.Identity.Sessions.CreateSession;

public record CreateSessionRequest(
    [property: Required, EmailAddress, MaxLength(254)]
    string Email,
    [property: Required, StringLength(128, MinimumLength = 12)]
    string Password,
    bool RememberMe)
{
    public CreateSessionCommand ToCommand(string userAgent, string? remoteIpAddress = null) =>
        new(Email, Password, RememberMe, userAgent, remoteIpAddress);
}
