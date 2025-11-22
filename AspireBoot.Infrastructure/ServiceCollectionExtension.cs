using AspireBoot.Domain;
using AspireBoot.Infrastructure.Postgres;
using AspireBoot.Infrastructure.Postgres.Repositories;
using AspireBoot.Infrastructure.Postgres.Repositories.Users;
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
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        string? postgresConnectionString = configuration.GetConnectionString("AspireBoot");
        ArgumentException.ThrowIfNullOrWhiteSpace(postgresConnectionString);
        serviceCollection.AddPostgres(postgresConnectionString);
        string? rabbitConnectionString = configuration.GetConnectionString("Rabbit");
        ArgumentException.ThrowIfNullOrWhiteSpace(rabbitConnectionString);
        serviceCollection.AddRabbit(rabbitConnectionString);
        string? redisConnectionString = configuration.GetConnectionString("Redis");
        ArgumentException.ThrowIfNullOrWhiteSpace(redisConnectionString);
        serviceCollection.AddRedis(redisConnectionString);

        return serviceCollection;
    }

    public static void AddPostgres(this IServiceCollection serviceCollection, string postgresConnectionString)
    {
        serviceCollection.AddDbContextPool<DatabaseContext>(x => x
            .UseNpgsql(new NpgsqlDataSourceBuilder(postgresConnectionString).EnableDynamicJson().Build(), y =>
                y.MigrationsAssembly("AspireBoot.Infrastructure").EnableRetryOnFailure()));
        serviceCollection.AddScoped(typeof(IBaseEntityRepository<>), typeof(BaseEntityRepository<>));
        serviceCollection.AddScoped<IUsersRepository, UsersRepository>();
        serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();

        switch (AppSettings.IsDevelopment)
        {
            case true:
            {
                using IServiceScope serviceScope = serviceCollection.BuildServiceProvider().CreateScope();
                serviceScope.ServiceProvider.GetRequiredService<DatabaseContext>().Database.Migrate();
                break;
            }
            case false:
                serviceCollection
                    .AddHealthChecks()
                    .AddDbContextCheck<DatabaseContext>()
                    .AddNpgSql(postgresConnectionString);
                break;
        }
    }

    public static void AddRabbit(this IServiceCollection serviceCollection, string rabbitConnectionString)
    {
        serviceCollection.AddSingleton<IConnectionFactory>(_ =>
            new ConnectionFactory { Uri = new Uri(rabbitConnectionString) });
        serviceCollection.AddSingleton<ConnectionProvider>();
        serviceCollection.AddSingleton<BasePublisher>();
        serviceCollection.AddHealthChecks().AddRabbitMQ(async x =>
            await x.GetRequiredService<ConnectionProvider>().GetConnectionAsync().ConfigureAwait(false));
    }

    public static void AddRedis(this IServiceCollection serviceCollection, string redisConnectionString)
    {
        serviceCollection.AddStackExchangeRedisCache(x => x.Configuration = redisConnectionString);
        serviceCollection.AddScoped<IRedisRepository, RedisRepository>();
        serviceCollection.AddHealthChecks().AddRedis(redisConnectionString);
    }
}
