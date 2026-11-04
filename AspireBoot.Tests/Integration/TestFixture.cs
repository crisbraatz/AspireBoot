using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using AspireBoot.Core.Integrations.Smtp;
using AspireBoot.Infrastructure;
using AspireBoot.Infrastructure.Integrations.Rustfs;
using AspireBoot.ServiceDefaults;
using AspireBoot.Worker;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Npgsql;
using Respawn;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace AspireBoot.Tests.Integration;

public sealed class TestFixture : IAsyncLifetime
{
    public ApiWebApplicationFactory ApiWebApplicationFactory { get; private set; } = null!;
    public IHost Host { get; private set; } = null!;

    public IContainer RustfsContainer { get; } = new ContainerBuilder("rustfs/rustfs:1.0.3")
        .WithEnvironment("RUSTFS_VOLUMES", "/data")
        .WithEnvironment("RUSTFS_ADDRESS", "0.0.0.0:9000")
        .WithEnvironment("RUSTFS_ACCESS_KEY", "aspireboot")
        .WithEnvironment("RUSTFS_SECRET_KEY", "aspireboot")
        .WithPortBinding(9000, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(httpWaitStrategy =>
            httpWaitStrategy.ForPort(9000).ForPath("/health")))
        .Build();

    private Dictionary<string, string?> _configuration = null!;
    private int _nextIpAddress;
    private Respawner _respawner = null!;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6-alpine")
        .WithDatabase("aspireboot-tests").Build();

    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:4.4.0-management-alpine")
        .WithUsername("aspireboot").WithPassword("aspireboot").Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:8.10.3-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(), _rabbit.StartAsync(), _redis.StartAsync(), RustfsContainer.StartAsync());

        _configuration = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["AllowedHosts"] = "api.tests",
            ["AspireBoot:Api:Frontend:Origin"] = "https://app.tests",
            ["AspireBoot:Api:RateLimiters:Auth:PermitLimit"] = "1000",
            ["AspireBoot:Api:RateLimiters:Reads:PermitLimit"] = "1000",
            ["AspireBoot:Api:RateLimiters:Writes:PermitLimit"] = "1000",
            ["AspireBoot:Core:EmailRequests:CooldownInSeconds"] = "1",
            ["AspireBoot:Core:EmailRequests:PermitLimit"] = "10",
            ["AspireBoot:Core:EmailRequests:WindowInMinutes"] = "1",
            ["AspireBoot:Infrastructure:Database:ApplyMigrationsOnStartup"] = "true",
            ["AspireBoot:Infrastructure:ObjectStorage:Endpoint"] =
                $"http://{RustfsContainer.Hostname}:{RustfsContainer.GetMappedPublicPort(9000)}",
            ["AspireBoot:Infrastructure:ObjectStorage:PublicEndpoint"] =
                $"http://{(RustfsContainer.Hostname is "127.0.0.1" ? "localhost" : RustfsContainer.Hostname)}:{RustfsContainer.GetMappedPublicPort(9000)}",
            ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
            ["ConnectionStrings:Rabbit"] = _rabbit.GetConnectionString(),
            ["ConnectionStrings:Redis"] = $"{_redis.GetConnectionString()},allowAdmin=true",
        };
        ApiWebApplicationFactory = new ApiWebApplicationFactory(_configuration);

        await using (AsyncServiceScope asyncServiceScope = ApiWebApplicationFactory.Services.CreateAsyncScope())
            await asyncServiceScope.ServiceProvider.GetRequiredService<IAmazonS3>()
                .PutBucketAsync(new PutBucketRequest
                {
                    BucketName = asyncServiceScope.ServiceProvider.GetRequiredService<RustfsOptions>().BucketName
                });

        WebApplicationBuilder webApplicationBuilder =
            WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        webApplicationBuilder.WebHost.UseUrls("http://127.0.0.1:0");
        webApplicationBuilder.AddServiceDefaults();
        string contentRootPath =
            ApiWebApplicationFactory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath;
        webApplicationBuilder.Configuration
            .AddJsonFile(Path.Combine(contentRootPath, "appsettings.Development.json"))
            .AddJsonFile(Path.Combine(contentRootPath, "..", "AspireBoot.Worker", "appsettings.Development.json"))
            .AddInMemoryCollection(_configuration);
        webApplicationBuilder.Services.AddWorkerInfrastructure(webApplicationBuilder.Configuration, true);
        webApplicationBuilder.AddWorker();
        webApplicationBuilder.Services.AddSingleton(
            ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>());

        WebApplication webApplication = webApplicationBuilder.Build();
        webApplication.MapDefaultEndpoints();
        Host = webApplication;

        await Host.StartAsync();

        await using NpgsqlConnection npgsqlConnection = new(_postgres.GetConnectionString());

        await npgsqlConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(
            npgsqlConnection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore = ["__EFMigrationsHistory", "DataProtectionKeys"]
            });
    }

    public ApiWebApplicationFactory CreateApiWebApplicationFactory(
        IReadOnlyDictionary<string, string?> overrides, Action<IServiceCollection>? action = null)
    {
        Dictionary<string, string?> configuration = new(_configuration, StringComparer.Ordinal);

        foreach ((string key, string? value) in overrides)
            configuration[key] = value;

        return new ApiWebApplicationFactory(configuration, action);
    }

    public IPAddress CreateIpAddress()
    {
        int value = Interlocked.Increment(ref _nextIpAddress);

        return IPAddress.Parse($"10.0.{value / 254}.{value % 254 + 1}");
    }

    public async Task ResetAsync()
    {
        await using NpgsqlConnection npgsqlConnection = new(_postgres.GetConnectionString());

        await npgsqlConnection.OpenAsync();

        using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(20));

        await using NpgsqlCommand npgsqlCommand = new(
            """
            SELECT (SELECT count(*) FROM messaging.wolverine_outgoing_envelopes)
                 + (SELECT count(*) FROM messaging.wolverine_incoming_envelopes WHERE status <> 'Handled')
            """,
            npgsqlConnection);

        while (Convert.ToInt64(
                   await npgsqlCommand.ExecuteScalarAsync(cancellationTokenSource.Token),
                   System.Globalization.CultureInfo.InvariantCulture) > 0)
            await Task.Delay(50, cancellationTokenSource.Token);

        ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().ClearReceivedCalls();

        await using AsyncServiceScope asyncServiceScope = ApiWebApplicationFactory.Services.CreateAsyncScope();

        await asyncServiceScope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>()
            .GetDatabase().ExecuteAsync("FLUSHDB");

        await _respawner.ResetAsync(npgsqlConnection);
    }

    public async ValueTask DisposeAsync()
    {
        await Host.StopAsync();

        Host.Dispose();

        await ApiWebApplicationFactory.DisposeAsync();
        await _postgres.DisposeAsync();
        await _rabbit.DisposeAsync();
        await _redis.DisposeAsync();
        await RustfsContainer.DisposeAsync();
    }
}
