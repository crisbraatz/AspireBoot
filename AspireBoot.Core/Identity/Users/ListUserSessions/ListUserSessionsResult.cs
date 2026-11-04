namespace AspireBoot.Core.Identity.Users.ListUserSessions;

public record ListUserSessionsResult(IReadOnlyList<ListUserSessionItemResult> Items, int Page, int Size, int Count);
