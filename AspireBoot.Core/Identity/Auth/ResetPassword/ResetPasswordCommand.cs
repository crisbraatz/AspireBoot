using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Auth.ResetPassword;

public record ResetPasswordCommand(string Email, string Password, string Token) : ICommand;
