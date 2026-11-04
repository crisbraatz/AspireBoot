using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Auth.RequestRecovery;

public class RequestRecoveryEmailPreparation(UserManager<IdentityUser> userManager)
    : EmailPreparation<RequestRecoveryEvent>
{
    public override bool ApplyRequestLimit => true;
    public override EmailRequestKind EmailRequestKind => EmailRequestKind.RequestRecovery;

    protected override bool CanSend(IdentityUser identityUser, User user) =>
        user.IsActive && identityUser.EmailConfirmed;

    protected override async Task<Result<EmailDetails>> PrepareAsync(
        IdentityUser identityUser,
        User user,
        RequestRecoveryEvent requestRecoveryEvent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string token = await userManager.GeneratePasswordResetTokenAsync(identityUser).ConfigureAwait(false);

        return Result<EmailDetails>.Success(EmailLayout.CreateWithLink(
            requestRecoveryEvent.Email,
            "Reset your AspireBoot password",
            "Let's get you back in.",
            "PASSWORD RECOVERY",
            "We received a request to reset your password. Use the button below to choose a new one.",
            "Reset password",
            $"{requestRecoveryEvent.FrontendOrigin.TrimEnd('/')}/auth/reset-password?email={Uri.EscapeDataString(requestRecoveryEvent.Email)}&token={Uri.EscapeDataString(token)}",
            "This link expires in one hour. If you didn't request this, your password hasn't changed and you can ignore this email."));
    }
}
