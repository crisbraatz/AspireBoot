using System.ComponentModel.DataAnnotations;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Auth.ChangeEmail;

public class ChangeEmailPreparation(UserManager<IdentityUser> userManager) : EmailPreparation<ChangeEmailEvent>
{
    public override bool ApplyRequestLimit => true;
    public override EmailRequestKind EmailRequestKind => EmailRequestKind.ChangeEmail;

    protected override async Task<Result<EmailDetails>> PrepareAsync(
        IdentityUser identityUser, User user, ChangeEmailEvent changeEmailEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string token = await userManager
            .GenerateChangeEmailTokenAsync(identityUser, changeEmailEvent.NewEmail).ConfigureAwait(false);

        return Result<EmailDetails>.Success(EmailLayout.CreateWithLink(
            changeEmailEvent.NewEmail,
            "Confirm your new AspireBoot email",
            "Confirm your new email address.",
            "EMAIL UPDATE",
            "Use the button below to confirm this address as the new email for your AspireBoot account.",
            "Confirm new email",
            $"{changeEmailEvent.FrontendOrigin.TrimEnd('/')}/auth/confirm-email-change?identityUserId={Uri.EscapeDataString(identityUser.Id)}&email={Uri.EscapeDataString(changeEmailEvent.NewEmail)}&token={Uri.EscapeDataString(token)}",
            "This link expires in one hour. Your current email remains unchanged until you confirm. If you didn't request this, ignore this email."));
    }

    protected override Result Validate(ChangeEmailEvent changeEmailEvent)
    {
        if (string.IsNullOrWhiteSpace(changeEmailEvent.NewEmail) ||
            changeEmailEvent.NewEmail.Length > 254 ||
            !new EmailAddressAttribute().IsValid(changeEmailEvent.NewEmail))
            return Result.Failure(
            [
                Failure.Validation(
                    "identity.auth.invalid-email",
                    "A valid new email is required.",
                    "newEmail")
            ]);

        return Result.Success();
    }
}
