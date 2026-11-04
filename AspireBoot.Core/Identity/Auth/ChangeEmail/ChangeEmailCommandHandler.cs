using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Messaging;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Auth.ChangeEmail;

public class ChangeEmailCommandHandler(
    IEmailRequestLimiter emailRequestLimiter,
    IEventRepository eventRepository,
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IUserRepository userRepository)
    : ICommandHandler<ChangeEmailCommand>
{
    public async Task<Result> HandleAsync(ChangeEmailCommand command, CancellationToken cancellationToken)
    {
        IdentityUser? identityUser = await userManager.GetUserAsync(command.ClaimsPrincipal).ConfigureAwait(false);

        if (identityUser is null ||
            await userRepository.FindByIdentityUserIdAsync
                (identityUser.Id, true, cancellationToken: cancellationToken).ConfigureAwait(false) is null ||
            !(await signInManager
                .CheckPasswordSignInAsync(identityUser, command.Password, true).ConfigureAwait(false)).Succeeded)
            return Result.Failure(
            [
                Failure.Unauthorized(
                    "identity.auth.invalid-credentials",
                    "Invalid credentials or account unavailable.")
            ]);

        ChangeEmailEvent changeEmailEvent = new(
            identityUser.Id,
            identityUser.Email!,
            IdentityConstants.NormalizeEmail(command.NewEmail.Trim()),
            command.FrontendOrigin);

        Result<bool> result = await emailRequestLimiter
            .CanSendAsync(identityUser.Id, changeEmailEvent.Recipient, changeEmailEvent.MessageId, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
            return Result.Failure(result.Failures!);

        if (!result.Value)
            return Result.Failure(
            [
                Failure.TooManyRequests(
                    "email.rate-limit.exceeded",
                    "Too many email requests were sent. Try again later.")
            ]);

        await eventRepository.AddAsync(changeEmailEvent, true, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
