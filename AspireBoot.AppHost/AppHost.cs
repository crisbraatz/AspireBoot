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
    .WithImage("postgres:17")
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
    .WithEndpoint("https", x =>
    {
        x.Port = 15671;
        x.TargetPort = 15672;
    })
    .WithDataVolume()
    .WithManagementPlugin(15673);

IResourceBuilder<RedisResource> redisResource = distributedApplicationBuilder
    .AddRedis(
        "Redis",
        password: distributedApplicationBuilder.AddParameter(
            "RedisPassword", configurationRoot["Redis:Password"] ?? "redis", secret: true))
    .WithImage("redis:8")
    .WithHostPort(6379)
    .WithDataVolume()
    .WithRedisCommander(x => x
            .WithEndpoint("http", y =>
            {
                y.Port = 6380;
                y.TargetPort = 8081;
            }),
        "RedisCommander");

IResourceBuilder<ProjectResource> apiServiceProjectResource = distributedApplicationBuilder
    .AddProject<Projects.AspireBoot_ApiService>("ApiService")
    .WithReference(postgresDatabaseResource)
    .WithReference(rabbitMqServerResource)
    .WithReference(redisResource)
    .WaitFor(postgresDatabaseResource)
    .WaitFor(rabbitMqServerResource)
    .WaitFor(redisResource)
    .WithEndpoint("https", x =>
    {
        x.Port = 5100;
        x.TargetPort = 5101;
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

distributedApplicationBuilder
    .AddNpmApp("Angular", "../AspireBoot.Angular")
    .WithReference(apiServiceProjectResource)
    .WithReference(workerServiceProjectResource)
    .WaitFor(apiServiceProjectResource)
    .WaitFor(workerServiceProjectResource)
    .WithHttpsEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

await distributedApplicationBuilder.Build().RunAsync().ConfigureAwait(false);
