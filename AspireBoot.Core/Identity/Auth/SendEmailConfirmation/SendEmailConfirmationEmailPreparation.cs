using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Auth.SendEmailConfirmation;

public class SendEmailConfirmationEmailPreparation(UserManager<IdentityUser> userManager)
    : EmailPreparation<SendEmailConfirmationEvent>
{
    public override bool ApplyRequestLimit => true;
    public override EmailRequestKind EmailRequestKind => EmailRequestKind.SendEmailConfirmation;

    protected override bool CanSend(IdentityUser identityUser, User user) =>
        user.IsActive && !identityUser.EmailConfirmed;

    protected override async Task<Result<EmailDetails>> PrepareAsync(
        IdentityUser identityUser,
        User user,
        SendEmailConfirmationEvent sendEmailConfirmationEvent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string token = await userManager.GenerateEmailConfirmationTokenAsync(identityUser).ConfigureAwait(false);

        return Result<EmailDetails>.Success(EmailLayout.CreateWithLink(
            sendEmailConfirmationEvent.Email,
            "Confirm your AspireBoot email",
            "One small step. You're almost there.",
            "VERIFY YOUR EMAIL",
            "Confirm your email address to finish setting up your AspireBoot account.",
            "Confirm email address",
            $"{sendEmailConfirmationEvent.FrontendOrigin.TrimEnd('/')}/auth/confirm-email?identityUserId={Uri.EscapeDataString(identityUser.Id)}&token={Uri.EscapeDataString(token)}",
            "This link expires in one hour. If you didn't request this, you can ignore this email."));
    }
}
