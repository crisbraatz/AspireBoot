using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "development";

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var postgresPassword = builder.AddParameter("postgres-password", config["Postgres:Password"] ?? "postgres");

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithImage("postgres:17")
    .WithEnvironment("PGHOST", config["Postgres:Host"] ?? "127.0.0.1")
    .WithEnvironment("PGPORT", config["Postgres:Port"] ?? "5432")
    .WithEnvironment("PGUSER", config["Postgres:Username"] ?? "postgres")
    .WithEnvironment("PGDATABASE", config["Postgres:Database"] ?? "aspireboot")
    .WithEnvironment("PGCONNECT_TIMEOUT", config["Postgres:ConnectTimeout"] ?? "60")
    .WithEnvironment("PGSSLMODE", config["Postgres:SslMode"] ?? "disable")
    .WithHostPort(5432)
    .WithDataVolume()
    .AddDatabase("aspireboot");

var redis = builder.AddRedis("redis")
    .WithImage("redis:7")
    .WithHostPort(6379)
    .WithDataVolume()
    .WithRedisCommander(x =>
    {
        x.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = 6380;
            endpoint.TargetPort = 8081;
        });
    });

var rabbitUsername = builder.AddParameter("rabbit-username", config["Rabbit:Username"] ?? "aspireboot");
var rabbitPassword = builder.AddParameter("rabbit-password", config["Rabbit:Password"] ?? "aspireboot");

var rabbit = builder.AddRabbitMQ("rabbit", rabbitUsername, rabbitPassword)
    .WithImage("library/rabbitmq:4-management")
    .WithEndpoint("https", endpoint =>
    {
        endpoint.Port = 15671;
        endpoint.TargetPort = 15672;
    })
    .WithDataVolume()
    .WithManagementPlugin(15673);

var api = builder.AddProject<Projects.AspireBoot_Api>("api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(rabbit)
    .WaitFor(postgres)
    .WaitFor(redis)
    .WaitFor(rabbit)
    .WithEndpoint("https", endpoint =>
    {
        endpoint.Port = 5100;
        endpoint.TargetPort = 5101;
    })
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.AspireBoot_Worker>("worker")
    .WithReference(postgres)
    .WithReference(rabbit)
    .WaitFor(postgres)
    .WaitFor(rabbit);

builder.AddNpmApp("angular", "../AspireBoot.Angular")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpsEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();