using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Messaging;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Auth.RequestRecovery;

public class RequestRecoveryCommandHandler(
    IEventRepository eventRepository,
    UserManager<IdentityUser> userManager,
    IUserRepository userRepository)
    : ICommandHandler<RequestRecoveryCommand>
{
    public async Task<Result> HandleAsync(RequestRecoveryCommand command, CancellationToken cancellationToken)
    {
        string email = IdentityConstants.NormalizeEmail(command.Email.Trim());

        IdentityUser? identityUser = await userManager.FindByEmailAsync(email).ConfigureAwait(false);

        if (identityUser is null ||
            !identityUser.EmailConfirmed ||
            await userRepository.FindByIdentityUserIdAsync(
                identityUser.Id, true, cancellationToken: cancellationToken).ConfigureAwait(false) is null)
            return Result.Success();

        await eventRepository
            .AddAsync(new RequestRecoveryEvent(identityUser.Id, email, command.FrontendOrigin), true, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success();
    }
}
