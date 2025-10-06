using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
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

            var properties = new BasicProperties { Headers = new Dictionary<string, object?>() };
            if (Activity.Current is not null)
                Propagators.DefaultTextMapPropagator.Inject(
                    new PropagationContext(Activity.Current.Context, Baggage.Current),
                    properties.Headers,
                    (headers, key, value) => headers[key] = Encoding.UTF8.GetBytes(value));

            await channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout, cancellationToken: token);
            await channel.BasicPublishAsync(
                exchange,
                string.Empty,
                false,
                properties,
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)),
                cancellationToken: token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "{class}.{method} - Error publishing message {message} to exchange {exchange}.",
                nameof(BasePublisher), nameof(PublishAsync), message, exchange);
        }
    }
}