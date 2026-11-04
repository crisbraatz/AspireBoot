using System.Text.Json;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Messaging;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Sessions.CreateSession;

public class CreateSessionCommandHandler(
    IEventRepository eventRepository,
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IUserRepository userRepository)
    : ICommandHandler<CreateSessionCommand>
{
    public async Task<Result> HandleAsync(CreateSessionCommand command, CancellationToken cancellationToken)
    {
        IdentityUser? identityUser = await userManager.FindByEmailAsync(command.Email.Trim()).ConfigureAwait(false);

        if (identityUser is null ||
            await userRepository.FindByIdentityUserIdAsync(
                identityUser.Id, true, true, cancellationToken).ConfigureAwait(false) is not { IsActive: true })
            return Result.Failure(
            [
                Failure.Unauthorized(
                    "identity.auth.invalid-credentials",
                    "Invalid credentials or account unavailable.")
            ]);

        TrackedDetails trackedDetails = TrackedDetails.Create(command.UserAgent, command.RemoteIpAddress);
        signInManager.Context.Items[IdentityConstants.SessionTrackedDetailsProperty] =
            JsonSerializer.Serialize(trackedDetails);
        SignInResult result;

        try
        {
            result = await signInManager
                .PasswordSignInAsync(identityUser, command.Password, command.RememberMe, true).ConfigureAwait(false);
        }
        finally
        {
            signInManager.Context.Items.Remove(IdentityConstants.SessionTrackedDetailsProperty);
        }

        if (!result.Succeeded)
            return Result.Failure(
            [
                Failure.Unauthorized(
                    "identity.auth.invalid-credentials",
                    "Invalid credentials or account unavailable.")
            ]);

        try
        {
            await eventRepository
                .AddAsync(
                    new CreateSessionEvent(identityUser.Id, identityUser.Email!, DateTimeOffset.UtcNow, trackedDetails),
                    true,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            await signInManager.SignOutAsync().ConfigureAwait(false);

            throw;
        }

        return Result.Success();
    }
}
