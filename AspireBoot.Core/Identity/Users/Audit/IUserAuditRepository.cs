namespace AspireBoot.Core.Identity.Users.Audit;

public interface IUserAuditRepository
{
    Task AddAsync(UserAuditEvent userAuditEvent, CancellationToken cancellationToken);
}
