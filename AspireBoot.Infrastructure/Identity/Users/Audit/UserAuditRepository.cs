using AspireBoot.Core.Identity.Users.Audit;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.EntityFrameworkCore;

namespace AspireBoot.Infrastructure.Identity.Users.Audit;

public class UserAuditRepository(AspireBootDbContext aspireBootDbContext) : IUserAuditRepository
{
    public async Task AddAsync(UserAuditEvent userAuditEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await aspireBootDbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                 INSERT INTO user_audits (message_id, user_id, action, updated_fields, created_at_utc, correlation_id)
                 VALUES ({userAuditEvent.MessageId},
                         {userAuditEvent.UserId},
                         {userAuditEvent.Action},
                         {userAuditEvent.UpdatedFields.ToArray()},
                         {userAuditEvent.CreatedAtUtc},
                         {userAuditEvent.CorrelationId})
                 ON CONFLICT (message_id) DO NOTHING
                 """,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
