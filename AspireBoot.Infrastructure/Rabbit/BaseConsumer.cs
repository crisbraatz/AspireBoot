using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AspireBoot.Infrastructure.Rabbit;

public abstract class BaseConsumer<T>(
    string exchange,
    string queue,
    IConnectionFactory connectionFactory,
    ILogger<BaseConsumer<T>> logger)
    : IHostedService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected abstract Task ProcessAsync(T? message);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout, cancellationToken: cancellationToken);
            var queueDeclareOk = await _channel.QueueDeclareAsync(cancellationToken: cancellationToken);
            var queueName = queueDeclareOk.QueueName;
            await _channel.QueueBindAsync(queueName, exchange, string.Empty, cancellationToken: cancellationToken);
            var asyncEventingBasicConsumer = new AsyncEventingBasicConsumer(_channel);
            asyncEventingBasicConsumer.ReceivedAsync += async (_, args) =>
            {
                await ProcessAsync(JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(args.Body.ToArray())));
                await _channel.BasicAckAsync(args.DeliveryTag, false, cancellationToken);
            };
            await _channel.BasicConsumeAsync(queueName, false, consumer: asyncEventingBasicConsumer,
                cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error receiving message from queue {queue} and exchange {exchange}",
                queue, exchange);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
            await _connection.DisposeAsync();
        }
    }
}