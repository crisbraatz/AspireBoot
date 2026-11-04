using AspireBoot.Core.Identity.Users.Audit;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Wolverine.Attributes;

namespace AspireBoot.Worker.Identity.Users.Audit;

public class UserAuditEventConsumer(IUserAuditRepository userAuditRepository)
{
    [WolverineHandler]
    [Transactional(typeof(AspireBootDbContext))]
    public async Task ConsumeAsync(UserAuditEvent userAuditEvent, CancellationToken cancellationToken) =>
        await userAuditRepository.AddAsync(userAuditEvent, cancellationToken).ConfigureAwait(false);
}
