using Microsoft.Extensions.Configuration;

IDistributedApplicationBuilder distributedApplicationBuilder = DistributedApplication.CreateBuilder(args);

IConfigurationRoot configurationRoot = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(
        $"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "development"}.json",
        optional: true,
        reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

IResourceBuilder<PostgresDatabaseResource> postgresDatabaseResource = distributedApplicationBuilder
    .AddPostgres(
        "Postgres",
        password: distributedApplicationBuilder.AddParameter(
            "PostgresPassword", configurationRoot["Postgres:Password"] ?? "postgres", secret: true))
    .WithImage("postgres:18")
    .WithEnvironment("PGHOST", configurationRoot["Postgres:Host"] ?? "127.0.0.1")
    .WithEnvironment("PGPORT", configurationRoot["Postgres:Port"] ?? "5432")
    .WithEnvironment("PGUSER", configurationRoot["Postgres:Username"] ?? "aspireboot")
    .WithEnvironment("PGDATABASE", configurationRoot["Postgres:Database"] ?? "aspireboot")
    .WithEnvironment("PGCONNECT_TIMEOUT", configurationRoot["Postgres:ConnectTimeout"] ?? "60")
    .WithEnvironment("PGSSLMODE", configurationRoot["Postgres:SslMode"] ?? "disable")
    .WithHostPort(5432)
    .WithDataVolume()
    .AddDatabase("aspireboot");

IResourceBuilder<RabbitMQServerResource> rabbitMqServerResource = distributedApplicationBuilder
    .AddRabbitMQ(
        "Rabbit",
        distributedApplicationBuilder.AddParameter(
            "RabbitUsername", configurationRoot["Rabbit:Username"] ?? "aspireboot"),
        distributedApplicationBuilder.AddParameter(
            "RabbitPassword", configurationRoot["Rabbit:Password"] ?? "rabbit", secret: true))
    .WithImage("library/rabbitmq:4-management")
    .WithDataVolume()
    .WithManagementPlugin(15672);

IResourceBuilder<RedisResource> redisResource = distributedApplicationBuilder
    .AddRedis(
        "Redis",
        password: distributedApplicationBuilder.AddParameter(
            "RedisPassword", configurationRoot["Redis:Password"] ?? "redis", secret: true))
    .WithImage("redis:8")
    .WithHostPort(6379)
    .WithDataVolume()
    .WithRedisCommander(containerName: "RedisCommander");

IResourceBuilder<ProjectResource> apiServiceProjectResource = distributedApplicationBuilder
    .AddProject<Projects.AspireBoot_ApiService>("ApiService")
    .WithReference(postgresDatabaseResource)
    .WithReference(rabbitMqServerResource)
    .WithReference(redisResource)
    .WaitFor(postgresDatabaseResource)
    .WaitFor(rabbitMqServerResource)
    .WaitFor(redisResource)
    .WithEndpoint("http", x =>
    {
        x.Port = 5101;
        x.TargetPort = 5100;
    })
    .WithHttpHealthCheck("/health");

IResourceBuilder<ProjectResource> workerServiceProjectResource = distributedApplicationBuilder
    .AddProject<Projects.AspireBoot_WorkerService>("WorkerService")
    .WithReference(postgresDatabaseResource)
    .WithReference(rabbitMqServerResource)
    .WithReference(redisResource)
    .WaitFor(postgresDatabaseResource)
    .WaitFor(rabbitMqServerResource)
    .WaitFor(redisResource);

IResourceBuilder<NodeAppResource> angularServiceNodeAppResource = distributedApplicationBuilder
    .AddNpmApp("Angular", "../AspireBoot.Angular", "start:prod")
    .WithReference(apiServiceProjectResource)
    .WithReference(workerServiceProjectResource)
    .WaitFor(apiServiceProjectResource)
    .WaitFor(workerServiceProjectResource)
    .WithEndpoint("http", x =>
    {
        x.Port = 4201;
        x.TargetPort = 4200;
    });

distributedApplicationBuilder
    .AddContainer("Caddy", "caddy:2")
    .WithBindMount("./Caddyfile", "/etc/caddy/Caddyfile")
    .WithBindMount("./localhost-cert.pem", "/etc/caddy/localhost-cert.pem")
    .WithBindMount("./localhost-key.pem", "/etc/caddy/localhost-key.pem")
    .WithHttpEndpoint(1443, 1443, "https")
    .WithReference(redisResource)
    .WithReference(rabbitMqServerResource)
    .WithReference(apiServiceProjectResource)
    .WithReference(workerServiceProjectResource)
    .WaitFor(redisResource)
    .WaitFor(rabbitMqServerResource)
    .WaitFor(apiServiceProjectResource)
    .WaitFor(angularServiceNodeAppResource);

await distributedApplicationBuilder.Build().RunAsync().ConfigureAwait(false);
