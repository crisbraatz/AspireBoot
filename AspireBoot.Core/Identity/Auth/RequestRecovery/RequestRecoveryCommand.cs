using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Auth.RequestRecovery;

public record RequestRecoveryCommand(string Email, string FrontendOrigin) : ICommand;
