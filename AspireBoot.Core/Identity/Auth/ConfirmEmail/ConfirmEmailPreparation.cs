using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Auth.ConfirmEmail;

public class ConfirmEmailPreparation : EmailPreparation<ConfirmEmailEvent>
{
    public override EmailRequestKind EmailRequestKind => EmailRequestKind.ConfirmEmail;

    protected override bool CanSend(IdentityUser identityUser, User user) =>
        user.IsActive && identityUser.EmailConfirmed;

    protected override Task<Result<EmailDetails>> PrepareAsync(
        IdentityUser identityUser, User user, ConfirmEmailEvent confirmEmailEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Result<EmailDetails>.Success(EmailLayout.Create(
            confirmEmailEvent.Email,
            "Welcome to AspireBoot",
            "You're all set.",
            "WELCOME TO ASPIREBOOT",
            EmailLayout.WithParagraph("Your email is confirmed and your AspireBoot account is ready.") +
            EmailLayout.WithParagraph(
                "You can now sign in, personalize your profile and manage your account. We're glad you're here.") +
            EmailLayout.WithNote(
                "Your account, your control. You can review your sessions and update your security settings at any time."),
            "Welcome to AspireBoot. Your email is confirmed and your account is ready. Sign in to personalize your profile and manage your account and security settings.")));
    }
}
