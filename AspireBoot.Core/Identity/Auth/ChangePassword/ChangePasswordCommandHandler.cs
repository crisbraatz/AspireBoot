using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions;
using AspireBoot.Core.Identity.Users;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Auth.ChangePassword;

public class ChangePasswordCommandHandler(
    ISessionsTicketStore sessionsTicketStore,
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IUserRepository userRepository)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        IdentityUser? identityUser = await userManager.GetUserAsync(command.ClaimsPrincipal).ConfigureAwait(false);

        if (identityUser is null ||
            await userRepository.FindByIdentityUserIdAsync(
                identityUser.Id, true, cancellationToken: cancellationToken).ConfigureAwait(false) is null ||
            !(await signInManager
                .CheckPasswordSignInAsync(identityUser, command.CurrentPassword, true).ConfigureAwait(false)).Succeeded)
            return Result.Failure(
            [
                Failure.Unauthorized(
                    "identity.auth.invalid-credentials",
                    "Invalid credentials or account unavailable.")
            ]);

        IdentityResult identityResult = await userRepository
            .TryChangePasswordAsync(identityUser, command.CurrentPassword, command.NewPassword, cancellationToken)
            .ConfigureAwait(false);

        if (!identityResult.Succeeded)
            return Result.Failure(identityResult.Errors
                .Select(identityError =>
                    Failure.Validation(
                        "identity.auth." + identityError.Code,
                        identityError.Code,
                        identityError.Code switch
                        {
                            "PasswordMismatch" => "currentPassword",
                            _ when identityError.Code.StartsWith("Password", StringComparison.Ordinal) => "newPassword",
                            _ => string.Empty
                        }))
                .ToArray());

        await sessionsTicketStore
            .RemoveOthersAsync(identityUser.Id, string.Empty, cancellationToken).ConfigureAwait(false);
        await signInManager.SignOutAsync().ConfigureAwait(false);

        return Result.Success();
    }
}
