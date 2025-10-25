using AspireBoot.Domain;
using AspireBoot.Infrastructure.Postgres;
using AspireBoot.Infrastructure.Postgres.Repositories;
using AspireBoot.Infrastructure.Rabbit;
using AspireBoot.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RabbitMQ.Client;

namespace AspireBoot.Infrastructure;

public static class ServiceCollectionExtension
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("aspireboot");
        if (string.IsNullOrWhiteSpace(postgresConnectionString))
            throw new Exception("Postgres Connection String not found");
        services.AddPostgres(postgresConnectionString);
        var rabbitConnectionString = configuration.GetConnectionString("rabbit");
        if (string.IsNullOrWhiteSpace(rabbitConnectionString))
            throw new Exception("Rabbit Connection String not found");
        services.AddRabbit(rabbitConnectionString);
        var redisConnectionString = configuration.GetConnectionString("redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
            throw new Exception("Redis Connection String not found");
        services.AddRedis(redisConnectionString);
    }

    public static void AddPostgres(this IServiceCollection services, string postgresConnectionString)
    {
        services.AddDbContextPool<DatabaseContext>(x => x
            .UseNpgsql(new NpgsqlDataSourceBuilder(postgresConnectionString).EnableDynamicJson().Build(), y =>
            {
                y.MigrationsAssembly("AspireBoot.Infrastructure");
                y.EnableRetryOnFailure();
            }));
        services.AddScoped(typeof(IBaseEntityRepository<>), typeof(BaseEntityRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        switch (AppSettings.IsDevelopment)
        {
            case true:
            {
                using var scope = services.BuildServiceProvider().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                context.Database.Migrate();
                break;
            }
            case false:
                services
                    .AddHealthChecks()
                    .AddDbContextCheck<DatabaseContext>()
                    .AddNpgSql(postgresConnectionString);
                break;
        }
    }

    public static void AddRabbit(this IServiceCollection services, string rabbitConnectionString)
    {
        services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory { Uri = new Uri(rabbitConnectionString) });
        services.AddSingleton<ConnectionProvider>();
        services.AddSingleton<BasePublisher>();
        services.AddHealthChecks().AddRabbitMQ(async x =>
            await x.GetRequiredService<ConnectionProvider>().GetConnectionAsync());
    }

    public static void AddRedis(this IServiceCollection services, string redisConnectionString)
    {
        services.AddStackExchangeRedisCache(x => x.Configuration = redisConnectionString);
        services.AddScoped<IRedisRepository, RedisRepository>();
        services.AddHealthChecks().AddRedis(redisConnectionString);
    }
}