using AspireBoot.Domain;
using AspireBoot.Infrastructure.Rabbit;
using AspireBoot.Worker.Consumers;
using RabbitMQ.Client;

namespace AspireBoot.Worker;

public static class ServiceCollectionExtension
{
    public static void AddWorker(this IServiceCollection services)
    {
        services.AddSingleton<IHostedService>(x =>
        {
            var connectionFactory = x.GetRequiredService<IConnectionFactory>();
            var logger = x.GetRequiredService<ILogger<BaseConsumer<string>>>();

            return new PingConsumer(
                exchange: AppSettings.RabbitPingConsumerExchange,
                queue: AppSettings.RabbitPingConsumerQueue,
                connectionFactory,
                logger);
        });
    }
}