namespace AspireBoot.Api.Identity.Users.ListUsers;

public record ListUserItemResponse(
    Guid Id,
    string Name,
    bool IsActive,
    bool IsAdministrator,
    Uri? AvatarUrl = null);
