using System.Net.Sockets;
using AspireBoot.Infrastructure.Observability;
using Npgsql;
using RabbitMQ.Client.Exceptions;
using StackExchange.Redis;

namespace AspireBoot.Tests.Unit.Infrastructure.Observability;

public class DependencyFailuresTests
{
    [Theory]
    [InlineData("dependency")]
    [InlineData("wrapped")]
    [InlineData("aggregate")]
    [InlineData("postgres-08006")]
    [InlineData("postgres-53000")]
    [InlineData("postgres-57P01")]
    [InlineData("postgres-57P02")]
    [InlineData("postgres-57P03")]
    [InlineData("npgsql-transient")]
    [InlineData("redis-connection")]
    [InlineData("redis-timeout")]
    [InlineData("rabbit-io")]
    [InlineData("rabbit-socket")]
    [InlineData("rabbit-timeout")]
    [InlineData("rabbit-wrapped")]
    [InlineData("rabbit-aggregate")]
    public void ShouldReturnTrueWhenRecognizeAnUnavailableDependency(string scenario)
    {
        bool unavailable = DependencyFailures.IsUnavailable(CreateException(scenario));

        Assert.True(unavailable);
    }

    [Theory]
    [InlineData(RedisErrorKind.Busy)]
    [InlineData(RedisErrorKind.ClusterDown)]
    [InlineData(RedisErrorKind.Loading)]
    [InlineData(RedisErrorKind.MasterDown)]
    [InlineData(RedisErrorKind.MaxClients)]
    [InlineData(RedisErrorKind.NoReplicas)]
    [InlineData(RedisErrorKind.OutOfMemory)]
    [InlineData(RedisErrorKind.ReadOnly)]
    [InlineData(RedisErrorKind.TryAgain)]
    public void ShouldReturnTrueAndRecognizeTemporaryRedisServerFailures(RedisErrorKind redisErrorKind)
    {
        bool unavailable = DependencyFailures.IsUnavailable(new RedisServerException(redisErrorKind, CommandFlags.None, "Unavailable."));

        Assert.True(unavailable);
    }

    [Theory]
    [InlineData("cancelled")]
    [InlineData("cancelled-with-inner")]
    [InlineData("empty-aggregate")]
    [InlineData("mixed-aggregate")]
    [InlineData("postgres-23505")]
    [InlineData("npgsql-permanent")]
    [InlineData("redis-wrongtype")]
    [InlineData("rabbit-cancelled")]
    [InlineData("rabbit-permanent")]
    [InlineData("rabbit-empty-aggregate")]
    [InlineData("rabbit-mixed-aggregate")]
    [InlineData("unexpected")]
    public void ShouldReturnFalseWhenPreservingCancellationAndPermanentFailures(string scenario)
    {
        bool unavailable = DependencyFailures.IsUnavailable(CreateException(scenario));

        Assert.False(unavailable);
    }

    private static Exception CreateException(string scenario) => scenario switch { "dependency" => new DependencyUnavailableException(DependencyKind.Caching, new IOException()), "wrapped" => new InvalidOperationException("Wrapped.", new RedisConnectionException(ConnectionFailureType.UnableToConnect, CommandFlags.None, "Unavailable.")), "aggregate" => new AggregateException(new RedisTimeoutException(CommandFlags.None, "Timeout.", CommandStatus.Unknown), new RedisConnectionException(ConnectionFailureType.UnableToConnect, CommandFlags.None, "Unavailable.")), _ when scenario.StartsWith("postgres-", StringComparison.Ordinal) => new PostgresException("Postgres failure.", "ERROR", "ERROR", scenario[9..]), "npgsql-transient" => new NpgsqlException("Unavailable.", new TimeoutException()), "npgsql-permanent" => new NpgsqlException("Invalid operation."), "redis-connection" => new RedisConnectionException(ConnectionFailureType.UnableToConnect, CommandFlags.None, "Unavailable."), "redis-timeout" => new RedisTimeoutException(CommandFlags.None, "Timeout.", CommandStatus.Unknown), "redis-wrongtype" => new RedisServerException(RedisErrorKind.WrongType, CommandFlags.None, "Wrong type."), "rabbit-io" => new BrokerUnreachableException(new IOException()), "rabbit-socket" => new BrokerUnreachableException(new SocketException()), "rabbit-timeout" => new BrokerUnreachableException(new TimeoutException()), "rabbit-wrapped" => new BrokerUnreachableException(new InvalidOperationException("Wrapped.", new IOException())), "rabbit-aggregate" => new BrokerUnreachableException(new AggregateException(new IOException(), new TimeoutException())), "rabbit-cancelled" => new BrokerUnreachableException(new OperationCanceledException()), "rabbit-permanent" => new BrokerUnreachableException(new InvalidOperationException()), "rabbit-empty-aggregate" => new BrokerUnreachableException(new AggregateException()), "rabbit-mixed-aggregate" => new BrokerUnreachableException(new AggregateException(new IOException(), new InvalidOperationException())), "cancelled" => new OperationCanceledException(), "cancelled-with-inner" => new OperationCanceledException("Cancelled.", new RedisConnectionException(ConnectionFailureType.UnableToConnect, CommandFlags.None, "Unavailable.")), "empty-aggregate" => new AggregateException(), "mixed-aggregate" => new AggregateException(new RedisTimeoutException(CommandFlags.None, "Timeout.", CommandStatus.Unknown), new InvalidOperationException()), _ => new InvalidOperationException() };
}
