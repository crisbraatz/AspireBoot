using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Auth.ConfirmEmailChange;

public record ConfirmEmailChangeCommand(string Email, string IdentityUserId, string Token) : ICommand;
