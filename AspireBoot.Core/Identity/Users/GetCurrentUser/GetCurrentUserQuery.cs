using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Users.GetCurrentUser;

public record GetCurrentUserQuery(string IdentityUserId) : IQuery<GetCurrentUserResult>;
