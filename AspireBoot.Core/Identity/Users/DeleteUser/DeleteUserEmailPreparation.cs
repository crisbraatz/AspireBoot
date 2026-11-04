using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Users.DeleteUser;

public class DeleteUserEmailPreparation : EmailPreparation<DeleteUserEvent>
{
    public override EmailRequestKind EmailRequestKind => EmailRequestKind.DeleteUser;

    protected override bool CanSend(IdentityUser identityUser, User user) => !user.IsActive;

    protected override Task<Result<EmailDetails>> PrepareAsync(
        IdentityUser identityUser, User user, DeleteUserEvent deleteUserEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Result<EmailDetails>.Success(EmailLayout.Create(
            deleteUserEvent.Email,
            "Your AspireBoot account was deleted",
            "Your account has been deactivated.",
            "ACCOUNT DELETION",
            EmailLayout.WithParagraph(
                "Your request to delete your AspireBoot account has been completed. You can no longer sign in and your sessions have been ended.") +
            EmailLayout.WithDetail("Requested at", EmailLayout.FormatDateTime(deleteUserEvent.OccurredAtUtc)) +
            EmailLayout.WithNote("Didn't request this? Contact support for help."),
            $"Your AspireBoot account has been deactivated and your sessions ended. Requested at: {EmailLayout.FormatDateTime(deleteUserEvent.OccurredAtUtc)}. If you didn't request this, contact support.")));
    }
}
