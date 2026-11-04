using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Auth.ResendEmailConfirmation;

public record ResendEmailConfirmationCommand(string Email, string FrontendOrigin) : ICommand;
