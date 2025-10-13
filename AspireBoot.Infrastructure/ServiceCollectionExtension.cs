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
        services.AddPostgres(configuration);
        services.AddRabbit(configuration);
        services.AddRedis(configuration);
    }

    private static void AddPostgres(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("aspireboot");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("Postgres Connection String not found");

        services.AddDbContextPool<DatabaseContext>(x => x
            .UseNpgsql(new NpgsqlDataSourceBuilder(connectionString).EnableDynamicJson().Build(), y =>
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
                    .AddNpgSql(connectionString);
                break;
        }
    }

    private static void AddRabbit(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("rabbit");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("Rabbit Connection String not found");

        services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory { Uri = new Uri(connectionString) });
        services.AddSingleton<ConnectionProvider>();
        services.AddSingleton<BasePublisher>();
        services.AddHealthChecks().AddRabbitMQ(async x =>
            await x.GetRequiredService<ConnectionProvider>().GetConnectionAsync());
    }

    private static void AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("redis");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("Redis Connection String not found");

        services.AddStackExchangeRedisCache(x => x.Configuration = connectionString);
        services.AddScoped<IRedisRepository, RedisRepository>();
        services.AddHealthChecks().AddRedis(connectionString);
    }
}