using AspireBoot.Domain;
using AspireBoot.Infrastructure.Rabbit;
using AspireBoot.Worker.Consumers;
using RabbitMQ.Client;

namespace AspireBoot.Worker;

public static class ServiceCollectionExtension
{
    public static void AddWorker(this IServiceCollection services) =>
        services.AddHostedService<PingConsumer>(x => new PingConsumer(
            exchange: AppSettings.RabbitPingConsumerExchange,
            queue: AppSettings.RabbitPingConsumerQueue,
            x.GetRequiredService<IConnectionFactory>(),
            x.GetRequiredService<IServiceScopeFactory>(),
            x.GetRequiredService<ILogger<BaseConsumer<string>>>()));
}