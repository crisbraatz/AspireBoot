using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Users.ListUsers;

public record ListUsersQuery(int Page, int Size, bool IsAdministrator) : IQuery<ListUsersResult>;
