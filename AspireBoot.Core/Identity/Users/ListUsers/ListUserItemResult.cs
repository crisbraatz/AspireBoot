namespace AspireBoot.Core.Identity.Users.ListUsers;

public record ListUserItemResult(
    Guid Id,
    string Name,
    bool IsActive,
    bool IsAdministrator,
    Uri? AvatarUrl = null);
