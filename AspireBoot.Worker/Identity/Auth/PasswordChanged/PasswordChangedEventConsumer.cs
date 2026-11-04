using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.PasswordChanged;
using AspireBoot.Core.Integrations.Smtp;
using Wolverine.Attributes;

namespace AspireBoot.Worker.Identity.Auth.PasswordChanged;

public class PasswordChangedEventConsumer(ICommandHandler<SendRequestedEmailCommand> commandHandler)
{
    [WolverineHandler]
    public async Task ConsumeAsync(PasswordChangedEvent passwordChangedEvent, CancellationToken cancellationToken)
    {
        Result result = await commandHandler
            .HandleAsync(new SendRequestedEmailCommand(passwordChangedEvent), cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
            return;

        EmailFailures.Throw(result.Failures);
    }
}
