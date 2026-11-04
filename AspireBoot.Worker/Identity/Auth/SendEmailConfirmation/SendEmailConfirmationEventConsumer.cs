using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.SendEmailConfirmation;
using AspireBoot.Core.Integrations.Smtp;
using Wolverine.Attributes;

namespace AspireBoot.Worker.Identity.Auth.SendEmailConfirmation;

public class SendEmailConfirmationEventConsumer(ICommandHandler<SendRequestedEmailCommand> commandHandler)
{
    [WolverineHandler]
    public async Task ConsumeAsync(
        SendEmailConfirmationEvent sendEmailConfirmationEvent, CancellationToken cancellationToken)
    {
        Result result = await commandHandler
            .HandleAsync(new SendRequestedEmailCommand(sendEmailConfirmationEvent), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
            return;

        EmailFailures.Throw(result.Failures);
    }
}
