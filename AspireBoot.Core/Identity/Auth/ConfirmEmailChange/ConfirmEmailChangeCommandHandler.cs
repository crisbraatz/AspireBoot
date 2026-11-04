using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Caching;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Auth.ConfirmEmailChange;

public class ConfirmEmailChangeCommandHandler(
    ICache cache,
    ISessionsTicketStore sessionsTicketStore,
    UserManager<IdentityUser> userManager,
    IUserRepository userRepository)
    : ICommandHandler<ConfirmEmailChangeCommand>
{
    public async Task<Result> HandleAsync(ConfirmEmailChangeCommand command, CancellationToken cancellationToken)
    {
        IdentityUser? identityUser = await userManager.FindByIdAsync(command.IdentityUserId).ConfigureAwait(false);

        if (identityUser is null)
            return Result.Failure(
            [
                Failure.Unauthorized(
                    "identity.auth.invalid-credentials",
                    "Invalid credentials or account unavailable.")
            ]);

        User? user = await userRepository
            .FindByIdentityUserIdAsync(identityUser.Id, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (user is null)
            return Result.Failure(
            [
                Failure.NotFound(
                    "identity.auth.user-not-found",
                    "User not found.")
            ]);

        IdentityResult result = await userRepository
            .TryUpdateEmailAsync(
                user, identityUser, IdentityConstants.NormalizeEmail(command.Email), command.Token, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
            return Result.Failure(result.Errors
                .Select(identityError =>
                    Failure.Validation(
                        "identity.auth." + identityError.Code,
                        identityError.Code,
                        identityError.Code switch
                        {
                            "DuplicateEmail" or "InvalidEmail" => "email",
                            "InvalidToken" => "token",
                            _ => string.Empty
                        }))
                .ToArray());

        await cache
            .RemoveAsync($"{IdentityConstants.UsersCachePrefix}{user.Id:N}", cancellationToken).ConfigureAwait(false);
        await sessionsTicketStore
            .RemoveOthersAsync(identityUser.Id, string.Empty, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
