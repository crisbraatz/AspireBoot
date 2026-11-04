using AspireBoot.Core.Integrations.Messaging;
using AspireBoot.Core.Integrations.Smtp;

namespace AspireBoot.Core.Identity.Auth.SendEmailConfirmation;

[EventType("identity.auth.send-email-confirmation")]
public record SendEmailConfirmationEvent(string IdentityUserId, string Email, string FrontendOrigin)
    : EmailRequestedEvent(IdentityUserId, Email, EmailRequestKind.SendEmailConfirmation);
