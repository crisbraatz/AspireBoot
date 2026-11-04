using System.Net.Sockets;
using Npgsql;
using RabbitMQ.Client.Exceptions;
using StackExchange.Redis;

namespace AspireBoot.Infrastructure.Observability;

public static class DependencyFailures
{
    public static bool IsUnavailable(Exception exception) => exception switch
    {
        AggregateException aggregateException => aggregateException.InnerExceptions.Count > 0 &&
                                                 aggregateException.InnerExceptions.All(IsUnavailable),
        DependencyUnavailableException => true,
        OperationCanceledException => false,
        _ => exception switch
        {
            PostgresException postgresException =>
                postgresException.SqlState.StartsWith("08", StringComparison.Ordinal) ||
                postgresException.SqlState.StartsWith("53", StringComparison.Ordinal) ||
                postgresException.SqlState is "57P01" or "57P02" or "57P03",
            NpgsqlException npgsqlException => npgsqlException.IsTransient,
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
            } => true,
            BrokerUnreachableException brokerUnreachableException => brokerUnreachableException.InnerException
                is { } innerException && IsTransportFailure(innerException),
            _ => exception.InnerException is { } innerException && IsUnavailable(innerException)
        }
    };

    private static bool IsTransportFailure(Exception exception) => exception switch
    {
        AggregateException aggregateException =>
            aggregateException.InnerExceptions.Count > 0 && aggregateException.InnerExceptions.All(IsTransportFailure),
        IOException or SocketException or TimeoutException => true,
        OperationCanceledException => false,
        _ => exception.InnerException is { } innerException && IsTransportFailure(innerException)
    };
}
