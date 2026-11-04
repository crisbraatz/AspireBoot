using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Sessions.CreateSession;

public record CreateSessionCommand(
    string Email,
    string Password,
    bool RememberMe,
    string UserAgent,
    string? RemoteIpAddress = null)
    : ICommand;
