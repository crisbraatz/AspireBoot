using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace AspireBoot.Infrastructure.Rabbit;

public class BasePublisher(IConnectionFactory factory, ILogger<BasePublisher> logger)
{
    public async Task PublishAsync(string exchange, object message, CancellationToken token = default)
    {
        try
        {
            await using var connection = await factory.CreateConnectionAsync(token);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: token);
            await channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout, cancellationToken: token);
            await channel.BasicPublishAsync(
                exchange,
                string.Empty,
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)),
                cancellationToken: token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error publishing message {message} to exchange {exchange}.",
                message, exchange);
        }
    }
}