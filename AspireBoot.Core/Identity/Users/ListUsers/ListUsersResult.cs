namespace AspireBoot.Core.Identity.Users.ListUsers;

public record ListUsersResult(IReadOnlyList<ListUserItemResult> Items, int Page, int Size, int Count);
