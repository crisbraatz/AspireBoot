using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions;
using AspireBoot.Core.Integrations.Caching;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Users.DeleteUser;

public class DeleteUserCommandHandler(
    ICache cache,
    ISessionsTicketStore sessionsTicketStore,
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IUserRepository userRepository)
    : ICommandHandler<DeleteUserCommand>
{
    public async Task<Result> HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        User? user = await userRepository
            .FindByIdAsync(command.CurrentUserId, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (user is null)
            return Result.Failure(
            [
                Failure.NotFound(
                    "identity.users.not-found",
                    "User not found.")
            ]);

        IdentityUser? identityUser = await userManager.FindByIdAsync(command.IdentityUserId).ConfigureAwait(false);

        if (identityUser is null ||
            user.IdentityUserId != identityUser.Id ||
            !(await signInManager
                .CheckPasswordSignInAsync(identityUser, command.Password, true).ConfigureAwait(false)).Succeeded)
            return Result.Failure(
            [
                Failure.Unauthorized(
                    "identity.auth.invalid-credentials",
                    "Invalid credentials or account unavailable.")
            ]);

        if (!(await userRepository.DeleteAsync(user, identityUser, cancellationToken).ConfigureAwait(false)).Succeeded)
            return Result.Failure(
            [
                Failure.Conflict(
                    "identity.users.delete-conflict",
                    "The account changed. Try again.")
            ]);

        await sessionsTicketStore
            .RemoveOthersAsync(identityUser.Id, string.Empty, cancellationToken).ConfigureAwait(false);
        await signInManager.SignOutAsync().ConfigureAwait(false);
        await cache
            .RemoveAsync($"{IdentityConstants.UsersCachePrefix}{user.Id:N}", cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
