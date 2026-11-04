using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Users.DeleteUser;

public record DeleteUserCommand(Guid CurrentUserId, string IdentityUserId, string Password) : ICommand;
