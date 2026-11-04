using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Sessions.DeleteSession;

public record DeleteSessionCommand(string IdentityUserId, string SessionId) : ICommand;
