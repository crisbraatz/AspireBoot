using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AspireBoot.Domain.Extensions;
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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ulong deliveryTag = 0;

        try
        {
            _connection = await connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
            _channel =
                await _connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await _channel
                .ExchangeDeclareAsync(
                    $"dlx.{exchange}", ExchangeType.Direct, durable: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await _channel
                .QueueDeclareAsync($"dlq.{queue}", durable: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await _channel
                .QueueBindAsync($"dlq.{queue}", $"dlx.{exchange}", "dlq", cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await _channel
                .ExchangeDeclareAsync(exchange, ExchangeType.Fanout, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            Dictionary<string, object?> arguments = new()
            {
                { "x-dead-letter-exchange", $"dlx.{exchange}" }, { "x-dead-letter-routing-key", "dlq" }
            };
            QueueDeclareOk queueDeclareOk = await _channel
                .QueueDeclareAsync(queue, true, false, false, arguments, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await _channel
                .QueueBindAsync(
                    queueDeclareOk.QueueName, exchange, string.Empty, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            AsyncEventingBasicConsumer asyncEventingBasicConsumer = new(_channel);
            asyncEventingBasicConsumer.ReceivedAsync += async (_, args) =>
            {
                PropagationContext propagationContext = Propagators.DefaultTextMapPropagator.Extract(
                    default,
                    args.BasicProperties.Headers,
                    (headers, key) =>
                    {
                        if (headers is not null && headers.TryGetValue(key, out object? value) && value is byte[] bytes)
                            return [Encoding.UTF8.GetString(bytes)];

                        return [];
                    });
                Baggage.Current = propagationContext.Baggage;

                using Activity? activity = new ActivitySource(activitySourceName)
                    .StartActivity(GetType().Name, ActivityKind.Consumer, propagationContext.ActivityContext);
                activity?.SetTag("rabbitmq.routing_key", args.RoutingKey);
                try
                {
                    using IServiceScope serviceScope = serviceScopeFactory.CreateScope();
                    await ProcessAsync(
                            JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(args.Body.ToArray())), serviceScope)
                        .ConfigureAwait(false);
                    await _channel.BasicAckAsync(args.DeliveryTag, false, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await _channel!.BasicRejectAsync(deliveryTag, false, cancellationToken).ConfigureAwait(false);

                    throw;
                }
            };
            await _channel
                .BasicConsumeAsync(
                    queueDeclareOk.QueueName,
                    false,
                    consumer: asyncEventingBasicConsumer,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
#pragma warning disable CA1031
        catch (Exception exception)
        {
            using (logger.BeginScope(new { Queue = queue, Exchange = exchange }))
                LoggerMessageExtension.LogBaseConsumerError(logger, queue, exchange, exception);

            await _channel!.BasicRejectAsync(deliveryTag, false, cancellationToken).ConfigureAwait(false);
        }
#pragma warning restore CA1031
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken).ConfigureAwait(false);
            await _channel.DisposeAsync().ConfigureAwait(false);
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken).ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
