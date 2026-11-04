using AspireBoot.Core.Integrations.Messaging;
using AspireBoot.Core.Integrations.Smtp;

namespace AspireBoot.Core.Identity.Sessions.CreateSession;

[EventType("identity.sessions.create-session")]
public record CreateSessionEvent(
    string IdentityUserId,
    string Email,
    DateTimeOffset OccurredAtUtc,
    TrackedDetails TrackedDetails)
    : EmailRequestedEvent(IdentityUserId, Email, EmailRequestKind.CreateSession);
