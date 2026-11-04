using AspireBoot.Core.Integrations.Messaging;
using AspireBoot.Core.Integrations.Smtp;

namespace AspireBoot.Core.Identity.Auth.ChangeEmail;

[EventType("identity.auth.change-email")]
public record ChangeEmailEvent(string IdentityUserId, string Email, string NewEmail, string FrontendOrigin)
    : EmailRequestedEvent(IdentityUserId, Email, EmailRequestKind.ChangeEmail)
{
    public override string Recipient => NewEmail;
}
