using AspireBoot.Domain.Meters;
using AspireBoot.Infrastructure.Rabbit;
using RabbitMQ.Client;

namespace AspireBoot.Worker.Consumers;

public class SampleConsumer(
    string exchange,
    string queue,
    IConnectionFactory connectionFactory,
    ILogger<BaseConsumer<string>> logger)
    : BaseConsumer<string>(exchange, queue, connectionFactory, logger)
{
    private readonly ILogger<BaseConsumer<string>> _logger = logger;

    protected override Task ProcessAsync(string? message)
    {
        PingMeter.PingRequests.Add(1);
        
        _logger.LogInformation("{class}.{method} - Message {message} consumed.",
            nameof(SampleConsumer), nameof(ProcessAsync), message);

        return Task.CompletedTask;
    }
}