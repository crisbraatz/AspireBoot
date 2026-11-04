using AspireBoot.Core.Identity.Users.Audit;

namespace AspireBoot.Infrastructure.Identity.Users.Audit;

public class UserAudit
{
    public Guid MessageId { get; private set; }
    public Guid UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public IReadOnlyList<string> UpdatedFields { get; private set; } = [];
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string? CorrelationId { get; private set; }

    private UserAudit()
    {
    }

    public UserAudit(UserAuditEvent userAuditEvent)
    {
        MessageId = userAuditEvent.MessageId;
        UserId = userAuditEvent.UserId;
        Action = userAuditEvent.Action;
        UpdatedFields = userAuditEvent.UpdatedFields;
        CreatedAtUtc = userAuditEvent.CreatedAtUtc;
        CorrelationId = userAuditEvent.CorrelationId;
    }
}
