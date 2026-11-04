using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Auth.Register;

public record RegisterCommand(string Email, string Password, string Name, string FrontendOrigin)
    : ICommand<RegisterResult>;
