using AspireBoot.Core.Integrations.Messaging;
using AspireBoot.Core.Integrations.Smtp;

namespace AspireBoot.Core.Identity.Auth.ConfirmEmail;

[EventType("identity.auth.confirm-email")]
public record ConfirmEmailEvent(string IdentityUserId, string Email)
    : EmailRequestedEvent(IdentityUserId, Email, EmailRequestKind.ConfirmEmail);
