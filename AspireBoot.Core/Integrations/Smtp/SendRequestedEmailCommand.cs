using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Integrations.Smtp;

public record SendRequestedEmailCommand(EmailRequestedEvent EmailRequestedEvent) : ICommand;
