using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Users.ListUserSessions;

public record ListUserSessionsQuery(Guid TargetUserId, Guid CurrentUserId, string CurrentSessionId, int Page, int Size)
    : IQuery<ListUserSessionsResult>;
