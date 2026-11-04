using System.Security.Cryptography;
using System.Text;
using AspireBoot.Core;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Integrations.Smtp;
using AspireBoot.Infrastructure.Observability;
using StackExchange.Redis;

namespace AspireBoot.Infrastructure.Integrations.Redis;

public class RedisEmailRequestLimiter(
    IConnectionMultiplexer connectionMultiplexer,
    EmailRequestsOptions emailRequestsOptions)
    : IEmailRequestLimiter
{
    public async Task<Result<bool>> CanSendAsync(
        string identityUserId, string email, Guid? requestId = null, CancellationToken cancellationToken = default)
    {
        using DependencyTrace dependencyTrace = new(DependencySources.Caching, "redis", "can_send");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            RedisKey[] redisKeys =
            [
                "identity:email:{requests}:account:" + Hash(identityUserId),
                "identity:email:{requests}:address:" + Hash(email.ToUpperInvariant())
            ];

            RedisResult redisResult = await connectionMultiplexer.GetDatabase()
                .ScriptEvaluateAsync(
                    """
                    local clock = redis.call('TIME')
                    local now = tonumber(clock[1]) * 1000 + math.floor(tonumber(clock[2]) / 1000)
                    for _, key in ipairs(KEYS) do
                        redis.call('ZREMRANGEBYSCORE', key, '-inf', now - tonumber(ARGV[2]))
                    end
                    if redis.call('ZSCORE', KEYS[1], ARGV[3]) and redis.call('ZSCORE', KEYS[2], ARGV[3]) then return 1 end
                    for _, key in ipairs(KEYS) do
                        if redis.call('ZCARD', key) >= tonumber(ARGV[1]) then return 0 end
                        local latest = redis.call('ZREVRANGE', key, 0, 0, 'WITHSCORES')
                        if #latest > 0 and now - tonumber(latest[2]) < tonumber(ARGV[4]) then return 0 end
                    end
                    for _, key in ipairs(KEYS) do
                        redis.call('ZADD', key, now, ARGV[3])
                        redis.call('PEXPIRE', key, ARGV[2])
                    end
                    return 1
                    """,
                    redisKeys,
                    [
                        emailRequestsOptions.PermitLimit,
                        (long)TimeSpan.FromMinutes(emailRequestsOptions.WindowInMinutes).TotalMilliseconds,
                        (requestId ?? Guid.CreateVersion7()).ToString("N"),
                        (long)TimeSpan.FromSeconds(emailRequestsOptions.CooldownInSeconds).TotalMilliseconds
                    ])
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            return Result<bool>.Success((long)redisResult is 1);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dependencyTrace.Cancel();

            throw;
        }
        catch (RedisException exception)
        {
            dependencyTrace.Fail(exception.GetType().Name, "Email request limiter unavailable.");

            throw;
        }
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
