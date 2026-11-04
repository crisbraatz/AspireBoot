using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Sessions.DeleteOtherSessions;

public record DeleteOtherSessionsCommand(string IdentityUserId, string CurrentSessionId) : ICommand;
