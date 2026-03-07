using RabbitMQ.Client;

namespace AspireBoot.Infrastructure.Rabbit;

public class ConnectionProvider(IConnectionFactory connectionFactory)
{
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync()
    {
        if (_connection is { IsOpen: true })
            return _connection;

        _connection = await connectionFactory.CreateConnectionAsync().ConfigureAwait(false);

        return _connection;
    }
}
