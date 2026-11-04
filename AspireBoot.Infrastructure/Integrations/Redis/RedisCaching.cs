using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Integrations.Caching;
using AspireBoot.Infrastructure.Observability;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AspireBoot.Infrastructure.Integrations.Redis;

public class RedisCaching(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisCaching> logger) : ICache
{
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task<Result> AddToSetAsync(
        string key, string value, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        using DependencyTrace dependencyTrace = new(DependencySources.Caching, "redis", "add_to_set");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            ITransaction transaction = _database.CreateTransaction();
            Task<bool> addTask = transaction.SetAddAsync(key, value);
            Task<bool> expireTask = transaction.KeyExpireAsync(key, timeToLive);

            if (!await transaction.ExecuteAsync().WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                dependencyTrace.Fail("RedisTransactionNotCommitted", "Redis transaction not committed.");
                DependencyLogs.LogDependencyWarning(
                    logger,
                    DependencyKind.Caching,
                    "add_to_set",
                    "RedisTransactionNotCommitted",
                    "Redis transaction not committed.");

                return EmptyDependencyFailure();
            }

            await Task.WhenAll(addTask, expireTask).WaitAsync(cancellationToken).ConfigureAwait(false);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dependencyTrace.Cancel();

            throw;
        }
        catch (RedisException exception)
        {
            dependencyTrace.Fail(exception.GetType().Name, "Redis integration failed.");

            if (!IsUnavailable(exception))
                throw;

            return EmptyDependencyFailure(exception, "add_to_set");
        }
    }

    public async Task<Result<string?>> GetAndRemoveAsync(string key, CancellationToken cancellationToken)
    {
        using DependencyTrace dependencyTrace = new(DependencySources.Caching, "redis", "get_and_remove");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            RedisValue redisValue =
                await _database.StringGetDeleteAsync(key).WaitAsync(cancellationToken).ConfigureAwait(false);

            return Result<string?>.Success(redisValue.IsNull ? null : redisValue.ToString());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dependencyTrace.Cancel();

            throw;
        }
        catch (RedisException exception)
        {
            dependencyTrace.Fail(exception.GetType().Name, "Redis integration failed.");

            if (!IsUnavailable(exception))
                throw;

            return StringDependencyFailure(exception, "get_and_remove");
        }
    }

    public async Task<Result<string?>> GetAsync(string key, CancellationToken cancellationToken)
    {
        using DependencyTrace dependencyTrace = new(DependencySources.Caching, "redis", "get");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            RedisValue redisValue =
                await _database.StringGetAsync(key).WaitAsync(cancellationToken).ConfigureAwait(false);

            return Result<string?>.Success(redisValue.IsNull ? null : redisValue.ToString());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dependencyTrace.Cancel();

            throw;
        }
        catch (RedisException exception)
        {
            dependencyTrace.Fail(exception.GetType().Name, "Redis integration failed.");

            if (!IsUnavailable(exception))
                throw;

            return StringDependencyFailure(exception, "get");
        }
    }

    public async Task<Result<IReadOnlySet<string>>> GetSetAsync(string key, CancellationToken cancellationToken)
    {
        using DependencyTrace dependencyTrace = new(DependencySources.Caching, "redis", "get_set");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            HashSet<string> values = new(StringComparer.Ordinal);

            foreach (RedisValue value in
                     await _database.SetMembersAsync(key).WaitAsync(cancellationToken).ConfigureAwait(false))
                if (!value.IsNull)
                    values.Add(value.ToString());

            return Result<IReadOnlySet<string>>.Success(values);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dependencyTrace.Cancel();

            throw;
        }
        catch (RedisException exception)
        {
            dependencyTrace.Fail(exception.GetType().Name, "Redis integration failed.");

            if (!IsUnavailable(exception))
                throw;

            return SetStringDependencyFailure(exception, "get_set");
        }
    }

    public async Task<Result> RemoveAsync(string key, CancellationToken cancellationToken)
    {
        using DependencyTrace dependencyTrace = new(DependencySources.Caching, "redis", "remove");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _database.KeyDeleteAsync(key).WaitAsync(cancellationToken).ConfigureAwait(false);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dependencyTrace.Cancel();

            throw;
        }
        catch (RedisException exception)
        {
            dependencyTrace.Fail(exception.GetType().Name, "Redis integration failed.");

            if (!IsUnavailable(exception))
                throw;

            return EmptyDependencyFailure(exception, "remove");
        }
    }

    public async Task<Result> RemoveFromSetAsync(string key, string value, CancellationToken cancellationToken)
    {
        using DependencyTrace dependencyTrace = new(DependencySources.Caching, "redis", "remove_from_set");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _database.SetRemoveAsync(key, value).WaitAsync(cancellationToken).ConfigureAwait(false);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dependencyTrace.Cancel();

            throw;
        }
        catch (RedisException exception)
        {
            dependencyTrace.Fail(exception.GetType().Name, "Redis integration failed.");

            if (!IsUnavailable(exception))
                throw;

            return EmptyDependencyFailure(exception, "remove_from_set");
        }
    }

    public async Task<Result> SetAsync(
        string key, string value, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        using DependencyTrace dependencyTrace = new(DependencySources.Caching, "redis", "set");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await _database
                    .StringSetAsync(key, value, timeToLive).WaitAsync(cancellationToken).ConfigureAwait(false))
                return Result.Success();

            dependencyTrace.Fail("RedisStringSetNotCommitted", "Redis string set not committed.");
            DependencyLogs.LogDependencyWarning(
                logger, DependencyKind.Caching, "set", "RedisStringSetNotCommitted", "Redis string set not committed.");

            return EmptyDependencyFailure();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dependencyTrace.Cancel();

            throw;
        }
        catch (RedisException exception)
        {
            dependencyTrace.Fail(exception.GetType().Name, "Redis integration failed.");

            if (!IsUnavailable(exception))
                throw;

            return EmptyDependencyFailure(exception, "set");
        }
    }

    private static Result EmptyDependencyFailure() => Result.Failure(
    [
        Failure.ServiceUnavailable(
            "caching.unavailable",
            "Caching is temporarily unavailable.")
    ]);

    private Result EmptyDependencyFailure(Exception exception, string method)
    {
        DependencyLogs.LogDependencyFailure(
            logger,
            DependencyKind.Caching,
            method,
            exception.GetType().Name,
            "Redis integration failed.",
            exception);

        return Result.Failure(
        [
            Failure.ServiceUnavailable(
                "caching.unavailable",
                "Caching is temporarily unavailable.")
        ]);
    }

    private static bool IsUnavailable(Exception exception) => exception is
        RedisConnectionException or RedisTimeoutException or RedisServerException
        {
            Kind:
            RedisErrorKind.Busy or
            RedisErrorKind.ClusterDown or
            RedisErrorKind.Loading or
            RedisErrorKind.MasterDown or
            RedisErrorKind.MaxClients or
            RedisErrorKind.NoReplicas or
            RedisErrorKind.OutOfMemory or
            RedisErrorKind.ReadOnly or
            RedisErrorKind.TryAgain
        };

    private Result<IReadOnlySet<string>> SetStringDependencyFailure(Exception exception, string method)
    {
        DependencyLogs.LogDependencyFailure(
            logger,
            DependencyKind.Caching,
            method,
            exception.GetType().Name,
            "Redis integration failed.",
            exception);

        return Result<IReadOnlySet<string>>.Failure(
        [
            Failure.ServiceUnavailable(
                "caching.unavailable",
                "Caching is temporarily unavailable.")
        ]);
    }

    private Result<string?> StringDependencyFailure(Exception exception, string method)
    {
        DependencyLogs.LogDependencyFailure(
            logger,
            DependencyKind.Caching,
            method,
            exception.GetType().Name,
            "Redis integration failed.",
            exception);

        return Result<string?>.Failure(
        [
            Failure.ServiceUnavailable(
                "caching.unavailable",
                "Caching is temporarily unavailable.")
        ]);
    }
}
