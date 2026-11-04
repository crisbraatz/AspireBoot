using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions;
using AspireBoot.Core.Identity.Users;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Auth.ResetPassword;

public class ResetPasswordCommandHandler(
    ISessionsTicketStore sessionsTicketStore,
    UserManager<IdentityUser> userManager,
    IUserRepository userRepository)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        IdentityUser? identityUser = await userManager.FindByEmailAsync(command.Email.Trim()).ConfigureAwait(false);

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
            .TryResetPasswordAsync(identityUser, command.Token, command.Password, cancellationToken)
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
                            _ when identityError.Code.StartsWith("Password", StringComparison.Ordinal) => "password",
                            _ => string.Empty
                        }))
                .ToArray());

        await sessionsTicketStore
            .RemoveOthersAsync(identityUser.Id, string.Empty, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
