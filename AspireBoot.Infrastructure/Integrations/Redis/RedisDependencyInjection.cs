using AspireBoot.Core;
using AspireBoot.Core.Integrations.Caching;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AspireBoot.Infrastructure.Integrations.Redis;

public static class RedisDependencyInjection
{
    public static void AddApiRedis(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddRedis(configuration);
        serviceCollection.AddScoped<ICache, RedisCaching>();
    }

    public static void AddWorkerRedis(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddRedis(configuration);
    }

    private static void AddRedis(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("Redis");
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        CoreOptions.Add(serviceCollection, configuration);

        serviceCollection.AddScoped<IEmailRequestLimiter, RedisEmailRequestLimiter>();
        serviceCollection.AddSingleton<IConnectionMultiplexer>(serviceProvider => RedisConnection.Connect(
            connectionString,
            serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("AspireBoot.Infrastructure.Integrations.Redis.RedisConnection")));
        serviceCollection.AddHealthChecks()
            .AddRedis(connectionString, "redis", HealthStatus.Unhealthy, ["ready", "dependency", "redis"]);
    }
}
