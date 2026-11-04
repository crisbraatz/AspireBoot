using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using AspireBoot.Core.Identity;
using AspireBoot.Core.Identity.Sessions;
using AspireBoot.Infrastructure.Observability;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;

namespace AspireBoot.Infrastructure.Integrations.Redis;

public class RedisSessionsTicketStore(
    IConnectionMultiplexer connectionMultiplexer,
    IDataProtectionProvider dataProtectionProvider)
    : ISessionsTicketStore
{
    private readonly IDataProtector _dataProtector =
        dataProtectionProvider.CreateProtector("AspireBoot.Identity.Sessions.v1");

    public Task<IReadOnlyList<IssuedSession>> ListAsync(
        string identityUserId, string? currentSessionId = null, CancellationToken cancellationToken = default) =>
        ExecuteAsync<IReadOnlyList<IssuedSession>>("list", async () =>
            {
                IDatabase database = connectionMultiplexer.GetDatabase();

                RedisValue[] redisValues = await database
                    .SetMembersAsync(IdentityConstants.GetAuthSessionsKey(identityUserId))
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                List<IssuedSession> issuedSessions = [];

                foreach (RedisValue redisValue in redisValues)
                {
                    string sessionId = redisValue.ToString();

                    AuthenticationTicket? authenticationTicket =
                        await GetAuthenticationTicketAsync(sessionId, cancellationToken).ConfigureAwait(false);

                    if (authenticationTicket is null || GetIdentityUserId(authenticationTicket) != identityUserId)
                    {
                        await database
                            .SetRemoveAsync(IdentityConstants.GetAuthSessionsKey(identityUserId), redisValue)
                            .WaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        continue;
                    }

                    issuedSessions.Add(new IssuedSession(
                        sessionId,
                        DateTimeOffset.Parse(
                            authenticationTicket.Properties.Items[IdentityConstants.SessionCreatedAtProperty]!,
                            CultureInfo.InvariantCulture),
                        authenticationTicket.Properties.ExpiresUtc!.Value,
                        sessionId == currentSessionId ? true : null,
                        GetTrackedDetails(authenticationTicket)));
                }

                return issuedSessions
                    .OrderByDescending(issuedSession => issuedSession.IsCurrent is true)
                    .ThenByDescending(issuedSession => issuedSession.CreatedAtUtc)
                    .ThenBy(issuedSession => issuedSession.Id, StringComparer.Ordinal)
                    .ToArray();
            },
            cancellationToken);

    public Task RemoveAsync(string key) =>
        ExecuteAsync("remove", async () =>
            {
                AuthenticationTicket? authenticationTicket =
                    await GetAuthenticationTicketAsync(key, CancellationToken.None).ConfigureAwait(false);
                await connectionMultiplexer.GetDatabase()
                    .KeyDeleteAsync(IdentityConstants.SessionsCachePrefix + key)
                    .WaitAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                if (authenticationTicket is not null)
                    await connectionMultiplexer.GetDatabase()
                        .SetRemoveAsync(
                            IdentityConstants.GetAuthSessionsKey(GetIdentityUserId(authenticationTicket)), key)
                        .WaitAsync(CancellationToken.None)
                        .ConfigureAwait(false);
            },
            CancellationToken.None);

    public Task RemoveOthersAsync(string identityUserId, string currentSessionId, CancellationToken cancellationToken)
        => ExecuteAsync("remove_others", async () =>
            {
                await connectionMultiplexer.GetDatabase()
                    .ScriptEvaluateAsync(
                        "local sessions = redis.call('SMEMBERS', KEYS[1]); " +
                        "for _, id in ipairs(sessions) do if id ~= ARGV[1] then " +
                        "redis.call('DEL', ARGV[2] .. id); " +
                        "redis.call('SREM', KEYS[1], id); " +
                        "end end; " +
                        "return 1",
                        [IdentityConstants.GetAuthSessionsKey(identityUserId)],
                        [currentSessionId, IdentityConstants.SessionsCachePrefix])
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            },
            cancellationToken);

    public Task RenewAsync(string key, AuthenticationTicket ticket) =>
        ExecuteAsync("renew", async () =>
            {
                AuthenticationTicket? authenticationTicket =
                    await GetAuthenticationTicketAsync(key, CancellationToken.None).ConfigureAwait(false);

                if (authenticationTicket is null)
                    return;

                if (GetIdentityUserId(authenticationTicket) != GetIdentityUserId(ticket))
                    throw new InvalidOperationException("A session cannot change its account.");

                ticket.Properties.Items[IdentityConstants.SessionCreatedAtProperty] =
                    authenticationTicket.Properties.Items[IdentityConstants.SessionCreatedAtProperty];
                ticket.Properties.Items[IdentityConstants.SessionIdProperty] = key;

                if (authenticationTicket.Properties.Items.TryGetValue(
                        IdentityConstants.SessionTrackedDetailsProperty, out string? sessionTrackedDetails))
                    ticket.Properties.Items[IdentityConstants.SessionTrackedDetailsProperty] = sessionTrackedDetails;

                DateTimeOffset absoluteExpiry = DateTimeOffset.Parse(
                        ticket.Properties.Items[IdentityConstants.SessionCreatedAtProperty]!,
                        CultureInfo.InvariantCulture)
                    .AddDays(7);

                if (ticket.Properties.ExpiresUtc > absoluteExpiry)
                    ticket.Properties.ExpiresUtc = absoluteExpiry;

                TimeSpan lifetime = ticket.Properties.ExpiresUtc!.Value - DateTimeOffset.UtcNow;

                if (lifetime <= TimeSpan.Zero)
                {
                    await connectionMultiplexer.GetDatabase()
                        .KeyDeleteAsync(IdentityConstants.SessionsCachePrefix + key)
                        .WaitAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    return;
                }

                await connectionMultiplexer.GetDatabase()
                    .StringSetAsync(
                        IdentityConstants.SessionsCachePrefix + key,
                        _dataProtector.Protect(TicketSerializer.Default.Serialize(ticket)),
                        lifetime,
                        When.Exists)
                    .WaitAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            },
            CancellationToken.None);

    public Task<AuthenticationTicket?> RetrieveAsync(string key) =>
        ExecuteAsync(
            "retrieve", () => GetAuthenticationTicketAsync(key, CancellationToken.None), CancellationToken.None);

    public Task<string> StoreAsync(AuthenticationTicket ticket) =>
        ExecuteAsync("store", async () =>
            {
                string sessionId = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
                DateTimeOffset now = DateTimeOffset.UtcNow;
                ticket.Properties.Items[IdentityConstants.SessionCreatedAtProperty] =
                    now.ToString("O", CultureInfo.InvariantCulture);
                ticket.Properties.Items[IdentityConstants.SessionIdProperty] = sessionId;
                ticket.Properties.ExpiresUtc ??= now.AddHours(12);

                if (ticket.Properties.ExpiresUtc > now.AddDays(7))
                    ticket.Properties.ExpiresUtc = now.AddDays(7);

                await connectionMultiplexer.GetDatabase()
                    .ScriptEvaluateAsync(
                        "redis.call('SET', KEYS[1], ARGV[1], 'PX', ARGV[2]); " +
                        "redis.call('SADD', KEYS[2], ARGV[3]); " +
                        "redis.call('PEXPIRE', KEYS[2], ARGV[4]); " +
                        "return 1",
                        [
                            IdentityConstants.SessionsCachePrefix + sessionId,
                            IdentityConstants.GetAuthSessionsKey(GetIdentityUserId(ticket))
                        ],
                        [
                            _dataProtector.Protect(TicketSerializer.Default.Serialize(ticket)),
                            Math.Max(
                                1L, (long)Math.Ceiling((ticket.Properties.ExpiresUtc.Value - now).TotalMilliseconds)),
                            sessionId,
                            (long)TimeSpan.FromDays(7).TotalMilliseconds
                        ])
                    .WaitAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                return sessionId;
            },
            CancellationToken.None);

    private static async Task ExecuteAsync(string method, Func<Task> func, CancellationToken cancellationToken) =>
        await ExecuteAsync(method, async () =>
                {
                    await func().ConfigureAwait(false);

                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);

    private static async Task<T> ExecuteAsync<T>(
        string method, Func<Task<T>> func, CancellationToken cancellationToken)
    {
        using DependencyTrace dependencyTrace = new(DependencySources.Caching, "redis", method);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await func().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dependencyTrace.Cancel();

            throw;
        }
        catch (RedisException exception)
        {
            dependencyTrace.Fail(exception.GetType().Name, "Redis integration failed.");

            throw;
        }
    }

    private async Task<AuthenticationTicket?> GetAuthenticationTicketAsync(
        string sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RedisValue redisValue = await connectionMultiplexer.GetDatabase()
            .StringGetAsync(IdentityConstants.SessionsCachePrefix + sessionId)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        if (redisValue.IsNull)
            return null;

        try
        {
            AuthenticationTicket? authenticationTicket =
                TicketSerializer.Default.Deserialize(_dataProtector.Unprotect((byte[]?)redisValue ?? []));

            if (authenticationTicket?.Properties.ExpiresUtc <= DateTimeOffset.UtcNow ||
                authenticationTicket is null ||
                !authenticationTicket.Properties.Items.TryGetValue(
                    IdentityConstants.SessionCreatedAtProperty, out string? created) ||
                !DateTimeOffset.TryParse(
                    created, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset createdAt) ||
                createdAt.AddDays(7) <= DateTimeOffset.UtcNow)
                return null;

            return authenticationTicket;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    private static string GetIdentityUserId(AuthenticationTicket authenticationTicket) =>
        authenticationTicket.Principal.FindFirst("sub")?.Value ??
        throw new InvalidOperationException("Identity user ID is missing.");

    private static TrackedDetails? GetTrackedDetails(AuthenticationTicket authenticationTicket)
    {
        if (!authenticationTicket.Properties.Items.TryGetValue(
                IdentityConstants.SessionTrackedDetailsProperty, out string? sessionTrackedDetails) ||
            sessionTrackedDetails is null)
            return null;

        try
        {
            return JsonSerializer.Deserialize<TrackedDetails>(sessionTrackedDetails);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
