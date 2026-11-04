using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Auth.PasswordChanged;

public class PasswordChangedEmailPreparation : EmailPreparation<PasswordChangedEvent>
{
    public override EmailRequestKind EmailRequestKind => EmailRequestKind.PasswordChanged;

    protected override Task<Result<EmailDetails>> PrepareAsync(
        IdentityUser identityUser,
        User user,
        PasswordChangedEvent passwordChangedEvent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Result<EmailDetails>.Success(EmailLayout.Create(
            passwordChangedEvent.Email,
            "Your AspireBoot password was changed",
            "Your password was updated.",
            "SECURITY UPDATE",
            EmailLayout.WithParagraph(
                "Your AspireBoot password was successfully changed. Your previous sessions have been signed out.") +
            EmailLayout.WithNote("Wasn't you? Reset your password from the sign-in page and contact support."),
            "Your AspireBoot password was changed. Your previous sessions have been signed out. If this wasn't you, reset your password from the sign-in page and contact support.")));
    }
}
