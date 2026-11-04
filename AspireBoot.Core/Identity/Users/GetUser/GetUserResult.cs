namespace AspireBoot.Core.Identity.Users.GetUser;

public record GetUserResult(
    Guid Id,
    string Name,
    bool IsActive,
    bool IsAdministrator,
    string? Email = null,
    Uri? AvatarUrl = null);
