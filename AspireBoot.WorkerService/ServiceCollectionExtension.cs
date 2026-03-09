using AspireBoot.Domain;
using AspireBoot.Infrastructure.Rabbit;
using AspireBoot.WorkerService.Consumers;
using RabbitMQ.Client;

namespace AspireBoot.WorkerService;

public static class ServiceCollectionExtension
{
    public static void AddWorker(this IServiceCollection serviceCollection) =>
        serviceCollection.AddHostedService<UserListRequestsCounterConsumer>(x => new UserListRequestsCounterConsumer(
            exchange: AppSettings.RabbitUserListRequestsCounterConsumerExchange,
            queue: AppSettings.RabbitUserListRequestsCounterConsumerQueue,
            x.GetRequiredService<IConnectionFactory>(),
            x.GetRequiredService<IServiceScopeFactory>(),
            x.GetRequiredService<ILogger<BaseConsumer<string>>>()));
}
