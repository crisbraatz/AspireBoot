using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AspireBoot.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;

namespace AspireBoot.Infrastructure.Rabbit;

public class BasePublisher(IConnectionFactory connectionFactory, ILogger<BasePublisher> logger)
{
    public async Task PublishAsync(string exchange, object message, CancellationToken token = default)
    {
        try
        {
            IConnection connection = await connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);
            await using (connection)
            {
                IChannel channel =
                    await connection.CreateChannelAsync(cancellationToken: token).ConfigureAwait(false);
                await using (channel)
                {
                    BasicProperties basicProperties = new() { Headers = new Dictionary<string, object?>() };
                    if (Activity.Current is not null)
                        Propagators.DefaultTextMapPropagator.Inject(
                            new PropagationContext(Activity.Current.Context, Baggage.Current),
                            basicProperties.Headers,
                            (headers, key, value) => headers[key] = Encoding.UTF8.GetBytes(value));
                    await channel
                        .ExchangeDeclareAsync(exchange, ExchangeType.Fanout, cancellationToken: token)
                        .ConfigureAwait(false);
                    await channel
                        .BasicPublishAsync(
                            exchange,
                            string.Empty,
                            false,
                            basicProperties,
                            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)),
                            cancellationToken: token)
                        .ConfigureAwait(false);
                }
            }
        }
#pragma warning disable CA1031
        catch (Exception exception)
        {
            using (logger.BeginScope(new { Message = message, Exchange = exchange }))
                LoggerMessageExtension.LogBasePublisherError(logger, exchange, exception);
        }
#pragma warning restore CA1031
    }
}
