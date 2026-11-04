using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Users.UpdateUser;

public record UpdateUserCommand(
    string IdentityUserId,
    string? NewName = null,
    ReadOnlyMemory<byte>? Avatar = null,
    string? AvatarContentType = null)
    : ICommand;
