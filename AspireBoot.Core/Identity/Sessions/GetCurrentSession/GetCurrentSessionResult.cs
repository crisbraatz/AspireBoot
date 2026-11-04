namespace AspireBoot.Core.Identity.Sessions.GetCurrentSession;

public record GetCurrentSessionResult(string IdentityUserId, Guid UserId, string Email, bool IsAdministrator);
