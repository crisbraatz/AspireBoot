using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Sessions.GetCurrentSession;

public record GetCurrentSessionQuery(string IdentityUserId, string Email, bool IsAdministrator)
    : IQuery<GetCurrentSessionResult>;
