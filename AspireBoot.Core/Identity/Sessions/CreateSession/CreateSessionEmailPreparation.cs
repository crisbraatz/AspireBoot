using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Sessions.CreateSession;

public class CreateSessionEmailPreparation : EmailPreparation<CreateSessionEvent>
{
    public override EmailRequestKind EmailRequestKind => EmailRequestKind.CreateSession;

    protected override Task<Result<EmailDetails>> PrepareAsync(
        IdentityUser identityUser,
        User user,
        CreateSessionEvent createSessionEvent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Result<EmailDetails>.Success(EmailLayout.Create(
            createSessionEvent.Email,
            "New sign-in to your AspireBoot account",
            "A new sign-in to your account.",
            "NEW SIGN-IN",
            EmailLayout.WithParagraph(
                "Your AspireBoot account was just used to sign in. Here are the details so you can check that it was you.") +
            EmailLayout.WithDetail("When", EmailLayout.FormatDateTime(createSessionEvent.OccurredAtUtc)) +
            EmailLayout.WithDetail("Browser", createSessionEvent.TrackedDetails.Browser) +
            EmailLayout.WithDetail("Operating system", createSessionEvent.TrackedDetails.OperatingSystem) +
            EmailLayout.WithDetail("Device", createSessionEvent.TrackedDetails.DeviceType) +
            EmailLayout.WithDetail("IP address", createSessionEvent.TrackedDetails.RemoteIpAddress ?? "Unavailable") +
            EmailLayout.WithNote(
                "Recognize this sign-in? No action is needed. If it wasn't you, reset your password and review your sessions. Browser and device details are reported by the client and may be approximate."),
            $"New sign-in to AspireBoot. When: {EmailLayout.FormatDateTime(createSessionEvent.OccurredAtUtc)}. Browser: {createSessionEvent.TrackedDetails.Browser}. Operating system: {createSessionEvent.TrackedDetails.OperatingSystem}. Device: {createSessionEvent.TrackedDetails.DeviceType}. IP: {createSessionEvent.TrackedDetails.RemoteIpAddress ?? "Unavailable"}. If this wasn't you, reset your password and review your sessions. Device details may be approximate.")));
    }
}
