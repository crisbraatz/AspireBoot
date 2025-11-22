using AspireBoot.Domain;
using AspireBoot.Domain.Meters;
using AspireBoot.Infrastructure.Extensions;
using AspireBoot.Infrastructure.Rabbit;
using RabbitMQ.Client;

namespace AspireBoot.WorkerService.Consumers;

public sealed class UserListRequestsCounterConsumer(
    string exchange,
    string queue,
    IConnectionFactory connectionFactory,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<BaseConsumer<string>> logger)
    : BaseConsumer<string>(
        nameof(ActivitySourceName.WorkerService),
        exchange,
        queue,
        connectionFactory,
        serviceScopeFactory,
        logger)
{
    private readonly ILogger<BaseConsumer<string>> _logger = logger;

    protected override Task ProcessAsync(string? message, IServiceScope serviceScope)
    {
        UsersMeter.UserListRequestsCounter.Add(1);

        using (_logger.BeginScope(new { Message = message }))
            LoggerMessageExtension.LogConsumerInformation(_logger, message!);

        return Task.CompletedTask;
    }
}
