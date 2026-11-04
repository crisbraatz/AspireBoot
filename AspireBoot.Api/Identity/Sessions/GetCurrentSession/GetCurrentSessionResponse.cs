namespace AspireBoot.Api.Identity.Sessions.GetCurrentSession;

public record GetCurrentSessionResponse(string IdentityUserId, Guid UserId, string Email, bool IsAdministrator);
