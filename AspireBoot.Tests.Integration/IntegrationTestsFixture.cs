using AspireBoot.ApiService;
using AspireBoot.Infrastructure;
using AspireBoot.Infrastructure.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace AspireBoot.Tests.Integration;

#pragma warning disable CA1515
public class IntegrationTestsFixture : IAsyncLifetime
#pragma warning restore CA1515
{
    public const string RequestedBy = "EXAMPLE@EMAIL.COM";
    public CancellationToken CancellationToken { get; } = CancellationToken.None;
    private IServiceProvider? _serviceProvider;
    private DatabaseContext? _databaseContext;

    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("integration-tests")
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder("library/rabbitmq:4").Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:8").Build();

    public async Task CommitAsync() =>
        await (_databaseContext?.SaveChangesAsync(CancellationToken)!).ConfigureAwait(false);

    public T GetRequiredService<T>() where T : notnull => _serviceProvider!.GetRequiredService<T>();

    public async Task ResetAsync()
    {
        await (_databaseContext?.Database.EnsureDeletedAsync(CancellationToken)!).ConfigureAwait(false);
        await (_databaseContext?.Database.EnsureCreatedAsync(CancellationToken)!).ConfigureAwait(false);
    }

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync(CancellationToken).ConfigureAwait(false);
        await _rabbitMqContainer.StartAsync(CancellationToken).ConfigureAwait(false);
        await _redisContainer.StartAsync(CancellationToken).ConfigureAwait(false);

        ServiceCollection serviceCollection = [];
        serviceCollection.AddPostgres(_postgreSqlContainer.GetConnectionString());
        serviceCollection.AddRabbit(_rabbitMqContainer.GetConnectionString());
        serviceCollection.AddRedis(_redisContainer.GetConnectionString());
        serviceCollection
            .AddLogging(x => x.AddConsole())
            .AddServices();

        _serviceProvider =
            new DefaultServiceProviderFactory(new ServiceProviderOptions()).CreateServiceProvider(serviceCollection);
        _databaseContext = GetRequiredService<DatabaseContext>();

        await _databaseContext.Database.EnsureDeletedAsync(CancellationToken).ConfigureAwait(false);
        await _databaseContext.Database.EnsureCreatedAsync(CancellationToken).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _postgreSqlContainer.DisposeAsync().ConfigureAwait(false);
        await _rabbitMqContainer.DisposeAsync().ConfigureAwait(false);
        await _redisContainer.DisposeAsync().ConfigureAwait(false);
    }
}
