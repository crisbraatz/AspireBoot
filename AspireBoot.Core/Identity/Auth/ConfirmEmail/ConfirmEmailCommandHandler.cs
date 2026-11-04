using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Auth.ConfirmEmail;

public class ConfirmEmailCommandHandler(UserManager<IdentityUser> userManager, IUserRepository userRepository)
    : ICommandHandler<ConfirmEmailCommand>
{
    public async Task<Result> HandleAsync(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        IdentityUser? identityUser = await userManager.FindByIdAsync(command.IdentityUserId).ConfigureAwait(false);

        if (identityUser is null ||
            await userRepository.FindByIdentityUserIdAsync(
                identityUser.Id, true, cancellationToken: cancellationToken).ConfigureAwait(false) is null)
            return Result.Failure(
            [
                Failure.Unauthorized(
                    "identity.auth.invalid-credentials",
                    "Invalid credentials or account unavailable.")
            ]);

        IdentityResult result = await userRepository
            .TryConfirmEmailAsync(identityUser, command.Token, cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
            return Result.Failure(result.Errors
                .Select(identityError =>
                    Failure.Validation(
                        "identity.auth." + identityError.Code,
                        identityError.Code,
                        identityError.Code switch
                        {
                            "InvalidToken" => "token",
                            _ => string.Empty
                        }))
                .ToArray());

        return Result.Success();
    }
}
