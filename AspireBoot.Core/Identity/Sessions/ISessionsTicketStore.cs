using Microsoft.AspNetCore.Authentication.Cookies;

namespace AspireBoot.Core.Identity.Sessions;

public interface ISessionsTicketStore : ITicketStore
{
    Task<IReadOnlyList<IssuedSession>> ListAsync(
        string identityUserId, string? currentSessionId = null, CancellationToken cancellationToken = default);

    Task RemoveOthersAsync(string identityUserId, string currentSessionId, CancellationToken cancellationToken);
}
