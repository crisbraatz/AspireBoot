using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ConfirmEmail;
using AspireBoot.Core.Integrations.Smtp;
using Wolverine.Attributes;

namespace AspireBoot.Worker.Identity.Auth.ConfirmEmail;

public class ConfirmEmailEventConsumer(ICommandHandler<SendRequestedEmailCommand> commandHandler)
{
    [WolverineHandler]
    public async Task ConsumeAsync(ConfirmEmailEvent confirmEmailEvent, CancellationToken cancellationToken)
    {
        Result result = await commandHandler
            .HandleAsync(new SendRequestedEmailCommand(confirmEmailEvent), cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
            return;

        EmailFailures.Throw(result.Failures);
    }
}
