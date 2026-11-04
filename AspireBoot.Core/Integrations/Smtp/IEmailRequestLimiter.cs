using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Integrations.Smtp;

public interface IEmailRequestLimiter
{
    Task<Result<bool>> CanSendAsync(
        string identityUserId, string email, Guid? requestId = null, CancellationToken cancellationToken = default);
}
