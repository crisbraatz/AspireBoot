using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Auth.ConfirmEmail;

public record ConfirmEmailCommand(string IdentityUserId, string Token) : ICommand;
