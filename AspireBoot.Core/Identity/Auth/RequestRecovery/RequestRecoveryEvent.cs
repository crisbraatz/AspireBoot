using AspireBoot.Core.Integrations.Messaging;
using AspireBoot.Core.Integrations.Smtp;

namespace AspireBoot.Core.Identity.Auth.RequestRecovery;

[EventType("identity.auth.request-recovery")]
public record RequestRecoveryEvent(string IdentityUserId, string Email, string FrontendOrigin)
    : EmailRequestedEvent(IdentityUserId, Email, EmailRequestKind.RequestRecovery);
