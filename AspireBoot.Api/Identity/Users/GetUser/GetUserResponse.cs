namespace AspireBoot.Api.Identity.Users.GetUser;

public record GetUserResponse(
    Guid Id,
    string Name,
    bool IsActive,
    bool IsAdministrator,
    string? Email = null,
    Uri? AvatarUrl = null);
