namespace AspireBoot.Core.Identity.Users.GetUser;

public record CachedUser(Guid Id, string Email, string Name, string? AvatarKey = null);
