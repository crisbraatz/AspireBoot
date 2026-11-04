using AspireBoot.Core.Integrations.Messaging;
using AspireBoot.Core.Integrations.Smtp;

namespace AspireBoot.Core.Identity.Users.DeleteUser;

[EventType("identity.users.delete-user")]
public record DeleteUserEvent(string IdentityUserId, string Email, DateTimeOffset OccurredAtUtc)
    : EmailRequestedEvent(IdentityUserId, Email, EmailRequestKind.DeleteUser);
