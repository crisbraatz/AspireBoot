using AspireBoot.Domain.Meters;
using AspireBoot.Infrastructure.Rabbit;
using RabbitMQ.Client;

namespace AspireBoot.Worker.Consumers;

public class PingConsumer(
    string exchange,
    string queue,
    IConnectionFactory connectionFactory,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<BaseConsumer<string>> logger)
    : BaseConsumer<string>("worker", exchange, queue, connectionFactory, serviceScopeFactory, logger)
{
    private readonly ILogger<BaseConsumer<string>> _logger = logger;

    protected override Task ProcessAsync(string? message, IServiceScope serviceScope)
    {
        PingMeter.PingRequests.Add(1);

        _logger.LogInformation("{class}.{method} - Message {message} consumed.",
            nameof(PingConsumer), nameof(ProcessAsync), message);

        return Task.CompletedTask;
    }
}