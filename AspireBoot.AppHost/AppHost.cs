using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(
        $"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "development"}.json",
        optional: true,
        reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var postgres = builder
    .AddPostgres(
        "postgres",
        password: builder.AddParameter("postgres-password", config["Postgres:Password"] ?? "postgres", secret: true))
    .WithImage("postgres:17")
    .WithEnvironment("PGHOST", config["Postgres:Host"] ?? "127.0.0.1")
    .WithEnvironment("PGPORT", config["Postgres:Port"] ?? "5432")
    .WithEnvironment("PGUSER", config["Postgres:Username"] ?? "aspireboot")
    .WithEnvironment("PGDATABASE", config["Postgres:Database"] ?? "aspireboot")
    .WithEnvironment("PGCONNECT_TIMEOUT", config["Postgres:ConnectTimeout"] ?? "60")
    .WithEnvironment("PGSSLMODE", config["Postgres:SslMode"] ?? "disable")
    .WithHostPort(5432)
    .WithDataVolume()
    .AddDatabase("aspireboot");

var rabbit = builder
    .AddRabbitMQ(
        "rabbit",
        builder.AddParameter("rabbit-username", config["Rabbit:Username"] ?? "aspireboot"),
        builder.AddParameter("rabbit-password", config["Rabbit:Password"] ?? "rabbit", secret: true))
    .WithImage("library/rabbitmq:4-management")
    .WithEndpoint("https", x =>
    {
        x.Port = 15671;
        x.TargetPort = 15672;
    })
    .WithDataVolume()
    .WithManagementPlugin(15673);

var redis = builder
    .AddRedis(
        "redis",
        password: builder.AddParameter("redis-password", config["Redis:Password"] ?? "redis", secret: true))
    .WithImage("redis:7")
    .WithHostPort(6379)
    .WithDataVolume()
    .WithRedisCommander(x => x
        .WithEndpoint("http", y =>
        {
            y.Port = 6380;
            y.TargetPort = 8081;
        }));

var api = builder.AddProject<Projects.AspireBoot_Api>("api")
    .WithReference(postgres)
    .WithReference(rabbit)
    .WithReference(redis)
    .WaitFor(postgres)
    .WaitFor(rabbit)
    .WaitFor(redis)
    .WithEndpoint("https", x =>
    {
        x.Port = 5100;
        x.TargetPort = 5101;
    })
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.AspireBoot_Worker>("worker")
    .WithReference(postgres)
    .WithReference(rabbit)
    .WithReference(redis)
    .WaitFor(postgres)
    .WaitFor(rabbit)
    .WaitFor(redis);

builder.AddNpmApp("angular", "../AspireBoot.Angular")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpsEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();