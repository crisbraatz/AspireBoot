using AspireBoot.Api;
using AspireBoot.Infrastructure.Postgres;
using AspireBoot.Infrastructure.Postgres.Repositories;
using AspireBoot.Infrastructure.Rabbit;
using AspireBoot.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace AspireBoot.Tests.Integration;

public class IntegrationTestsFixture : IAsyncLifetime
{
    public const string RequestedBy = "example@email.com";
    public readonly CancellationToken Token = CancellationToken.None;
    private IServiceProvider? _provider;
    private DatabaseContext? _context;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("integration-tests")
        .Build();

    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder()
        .WithImage("library/rabbitmq:4")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7")
        .Build();

    public async Task CommitAsync() => await _context?.SaveChangesAsync(Token)!;

    public T GetRequiredService<T>() where T : notnull => _provider!.GetRequiredService<T>();

    public async Task ResetAsync()
    {
        await _context?.Database.EnsureDeletedAsync(Token)!;
        await _context?.Database.EnsureCreatedAsync(Token)!;
    }

    private void AddPostgres(ServiceCollection services)
    {
        services.AddDbContextPool<DatabaseContext>(x => x
            .UseNpgsql(new NpgsqlDataSourceBuilder(_postgres.GetConnectionString()).EnableDynamicJson().Build(), y =>
            {
                y.MigrationsAssembly("AspireBoot.Infrastructure");
                y.EnableRetryOnFailure();
            }));
        services.AddScoped(typeof(IBaseEntityRepository<>), typeof(BaseEntityRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private void AddRabbit(IServiceCollection services)
    {
        services.AddSingleton<IConnectionFactory>(_ =>
            new ConnectionFactory { Uri = new Uri(_rabbit.GetConnectionString()) });
        services.AddSingleton<ConnectionProvider>();
        services.AddSingleton<BasePublisher>();
    }

    private void AddRedis(IServiceCollection services)
    {
        services.AddStackExchangeRedisCache(x => x.Configuration = _redis.GetConnectionString());
        services.AddScoped<IRedisRepository, RedisRepository>();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync(Token);
        await _rabbit.StartAsync(Token);
        await _redis.StartAsync(Token);

        var services = new ServiceCollection();
        AddPostgres(services);
        AddRabbit(services);
        AddRedis(services);
        services.AddLogging(x => x.AddConsole());
        services.AddServices();

        _provider = new DefaultServiceProviderFactory(new ServiceProviderOptions()).CreateServiceProvider(services);
        _context = GetRequiredService<DatabaseContext>();

        await _context.Database.EnsureDeletedAsync(Token);
        await _context.Database.EnsureCreatedAsync(Token);
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbit.DisposeAsync();
        await _redis.DisposeAsync();
    }
}