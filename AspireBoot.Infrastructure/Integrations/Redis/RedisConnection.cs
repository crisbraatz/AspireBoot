using System.Runtime.CompilerServices;
using AspireBoot.Infrastructure.Observability;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AspireBoot.Infrastructure.Integrations.Redis;

public static class RedisConnection
{
    public static IConnectionMultiplexer Connect(string connectionString, ILogger logger)
    {
        ConfigurationOptions configurationOptions = ConfigurationOptions.Parse(connectionString);
        configurationOptions.AbortOnConnectFail = false;

        using DependencyTrace dependencyTrace = new(DependencySources.Caching, "redis", "connect");

        ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
        StrongBox<int> disconnected = new();
        connectionMultiplexer.ConnectionFailed += (_, connectionFailedEventArgs) =>
        {
            if (Interlocked.Exchange(ref disconnected.Value, 1) is 0)
                DependencyLogs.LogDependencyFailure(
                    logger,
                    DependencyKind.Caching,
                    "connect",
                    "RedisConnectionFailed",
                    "Redis connection failed.",
                    connectionFailedEventArgs.Exception);
        };
        connectionMultiplexer.ConnectionRestored += (_, _) =>
        {
            if (connectionMultiplexer.IsConnected && Interlocked.Exchange(ref disconnected.Value, 0) is 1)
                DependencyLogs.LogDependencyWarning(
                    logger, DependencyKind.Caching, "connect", "RedisConnectionRestored", "Redis connection restored.");
        };

        if (connectionMultiplexer.IsConnected)
            return connectionMultiplexer;

        dependencyTrace.Fail("RedisDisconnected", "Redis disconnected.");

        if (Interlocked.Exchange(ref disconnected.Value, 1) is 0)
            DependencyLogs.LogDependencyFailure(
                logger, DependencyKind.Caching, "connect", "RedisDisconnected", "Redis disconnected.");

        return connectionMultiplexer;
    }
}
