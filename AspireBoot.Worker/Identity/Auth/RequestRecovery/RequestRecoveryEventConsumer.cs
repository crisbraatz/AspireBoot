using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.RequestRecovery;
using AspireBoot.Core.Integrations.Smtp;
using Wolverine.Attributes;

namespace AspireBoot.Worker.Identity.Auth.RequestRecovery;

public class RequestRecoveryEventConsumer(ICommandHandler<SendRequestedEmailCommand> commandHandler)
{
    [WolverineHandler]
    public async Task ConsumeAsync(RequestRecoveryEvent requestRecoveryEvent, CancellationToken cancellationToken)
    {
        Result result = await commandHandler
            .HandleAsync(new SendRequestedEmailCommand(requestRecoveryEvent), cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
            return;

        EmailFailures.Throw(result.Failures);
    }
}
