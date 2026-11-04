using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Users.GetUser;

public record GetUserQuery(Guid TargetUserId, Guid CurrentUserId, bool IsAdministrator) : IQuery<GetUserResult>;
