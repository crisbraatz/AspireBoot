using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AspireBoot.Infrastructure.Rabbit;

public abstract class BaseConsumer<T>(
    string activitySourceName,
    string exchange,
    string queue,
    IConnectionFactory connectionFactory,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<BaseConsumer<T>> logger)
    : IHostedService
{
    private IChannel? _channel;
    private IConnection? _connection;

    protected abstract Task ProcessAsync(T? message, IServiceScope serviceScope);

    public async Task StartAsync(CancellationToken token)
    {
        try
        {
            _connection = await connectionFactory.CreateConnectionAsync(token);
            _channel = await _connection.CreateChannelAsync(cancellationToken: token);
            await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout, cancellationToken: token);
            var queueDeclareOk = await _channel.QueueDeclareAsync(queue, cancellationToken: token);
            var queueName = queueDeclareOk.QueueName;
            await _channel.QueueBindAsync(queueName, exchange, string.Empty, cancellationToken: token);
            var asyncEventingBasicConsumer = new AsyncEventingBasicConsumer(_channel);
            asyncEventingBasicConsumer.ReceivedAsync += async (_, args) =>
            {
                var context = Propagators.DefaultTextMapPropagator.Extract(
                    default,
                    args.BasicProperties.Headers,
                    (headers, key) =>
                    {
                        if (headers is not null && headers.TryGetValue(key, out var value) && value is byte[] bytes)
                            return [Encoding.UTF8.GetString(bytes)];

                        return [];
                    });
                Baggage.Current = context.Baggage;

                using var activity = new ActivitySource(activitySourceName)
                    .StartActivity(GetType().Name, ActivityKind.Consumer, context.ActivityContext);
                activity?.SetTag("rabbitmq.routing_key", args.RoutingKey);
                using var serviceScope = serviceScopeFactory.CreateScope();
                await ProcessAsync(
                    JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(args.Body.ToArray())), serviceScope);
                await _channel.BasicAckAsync(args.DeliveryTag, false, token);
            };
            await _channel.BasicConsumeAsync(
                queueName, false, consumer: asyncEventingBasicConsumer, cancellationToken: token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "{class}.{method} - Error receiving message from queue {queue} and exchange {exchange}",
                nameof(BaseConsumer<T>), nameof(StartAsync), queue, exchange);
        }
    }

    public async Task StopAsync(CancellationToken token)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(token);
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(token);
            await _connection.DisposeAsync();
        }
    }
}