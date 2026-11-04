using AspireBoot.Core.Integrations.Messaging;
using AspireBoot.Core.Integrations.Smtp;

namespace AspireBoot.Core.Identity.Auth.PasswordChanged;

[EventType("identity.auth.password-changed")]
public record PasswordChangedEvent(string IdentityUserId, string Email)
    : EmailRequestedEvent(IdentityUserId, Email, EmailRequestKind.PasswordChanged);
