using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.DeleteUser;
using AspireBoot.Core.Integrations.Smtp;
using Wolverine.Attributes;

namespace AspireBoot.Worker.Identity.Users.DeleteUser;

public class DeleteUserEventConsumer(ICommandHandler<SendRequestedEmailCommand> commandHandler)
{
    [WolverineHandler]
    public async Task ConsumeAsync(DeleteUserEvent deleteUserEvent, CancellationToken cancellationToken)
    {
        Result result = await commandHandler
            .HandleAsync(new SendRequestedEmailCommand(deleteUserEvent), cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
            return;

        EmailFailures.Throw(result.Failures);
    }
}
