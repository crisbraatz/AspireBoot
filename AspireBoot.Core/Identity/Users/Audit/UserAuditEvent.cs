using AspireBoot.Core.Integrations.Messaging;

namespace AspireBoot.Core.Identity.Users.Audit;

[EventType("identity.users.audit")]
public record UserAuditEvent(Guid UserId, string Action, IReadOnlyList<string> UpdatedFields) : AbstractEvent
{
    public const string Update = "update";
}
