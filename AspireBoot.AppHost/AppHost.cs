IDistributedApplicationBuilder distributedApplicationBuilder = DistributedApplication.CreateBuilder(args);

string? databasePassword = distributedApplicationBuilder.Configuration["AspireBoot:Database:Password"];
ArgumentException.ThrowIfNullOrWhiteSpace(databasePassword);
string? databaseUsername = distributedApplicationBuilder.Configuration["AspireBoot:Database:Username"];
ArgumentException.ThrowIfNullOrWhiteSpace(databaseUsername);
IResourceBuilder<PostgresServerResource> postgresServerResource = distributedApplicationBuilder
    .AddPostgres(
        "postgres-server",
        distributedApplicationBuilder.AddParameter("postgres-username", databaseUsername),
        distributedApplicationBuilder.AddParameter("postgres-password", databasePassword, secret: true),
        5432)
    .WithDataVolume("aspireboot-postgres")
    .WithImageTag("18.6-alpine")
    .WithLifetime(ContainerLifetime.Persistent);
IResourceBuilder<PostgresDatabaseResource> postgresDatabaseResource =
    postgresServerResource.AddDatabase("postgres", "aspireboot");

string? messagingPassword = distributedApplicationBuilder.Configuration["AspireBoot:Messaging:Password"];
ArgumentException.ThrowIfNullOrWhiteSpace(messagingPassword);
string? messagingUsername = distributedApplicationBuilder.Configuration["AspireBoot:Messaging:Username"];
ArgumentException.ThrowIfNullOrWhiteSpace(messagingUsername);
IResourceBuilder<RabbitMQServerResource> rabbitResource = distributedApplicationBuilder
    .AddRabbitMQ(
        "rabbitmq",
        distributedApplicationBuilder.AddParameter("rabbit-username", messagingUsername),
        distributedApplicationBuilder.AddParameter("rabbit-password", messagingPassword, secret: true),
        5672)
    .WithDataVolume("aspireboot-rabbitmq")
    .WithImageTag("4.4.0-management-alpine")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin(15672)
    .WaitFor(postgresDatabaseResource);

IResourceBuilder<RedisResource> redisResource = distributedApplicationBuilder
    .AddRedis("redis", 6379)
    .WithDataVolume("aspireboot-redis")
    .WithImageTag("8.10.3-alpine")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithRedisCommander(containerName: "redis-commander")
    .WaitFor(rabbitResource);

string? objectStorageAccessKey = distributedApplicationBuilder.Configuration["AspireBoot:ObjectStorage:AccessKey"];
ArgumentException.ThrowIfNullOrWhiteSpace(objectStorageAccessKey);
string? objectStorageOrigin = distributedApplicationBuilder.Configuration["AspireBoot:ObjectStorage:Origin"];
ArgumentException.ThrowIfNullOrWhiteSpace(objectStorageOrigin);
string? objectStorageSecretKey = distributedApplicationBuilder.Configuration["AspireBoot:ObjectStorage:SecretKey"];
ArgumentException.ThrowIfNullOrWhiteSpace(objectStorageSecretKey);
IResourceBuilder<ParameterResource> objectStorageSecretKeyParameterResource =
    distributedApplicationBuilder.AddParameter("rustfs-secret-key", objectStorageSecretKey, secret: true);
IResourceBuilder<ContainerResource> rustfsResource = distributedApplicationBuilder
    .AddContainer("rustfs", "rustfs/rustfs", "1.0.3")
    .WithEnvironment("RUSTFS_ACCESS_KEY", objectStorageAccessKey)
    .WithEnvironment("RUSTFS_ADDRESS", "0.0.0.0:9000")
    .WithEnvironment("RUSTFS_CONSOLE_ADDRESS", "0.0.0.0:9001")
    .WithEnvironment("RUSTFS_CONSOLE_CORS_ALLOWED_ORIGINS", objectStorageOrigin)
    .WithEnvironment("RUSTFS_CONSOLE_ENABLE", "true")
    .WithEnvironment("RUSTFS_SECRET_KEY", objectStorageSecretKeyParameterResource)
    .WithEnvironment("RUSTFS_VOLUMES", "/data")
    .WithHttpEndpoint(9000, 9000, "api")
    .WithHttpEndpoint(9001, 9001, "console")
    .WithHttpHealthCheck("/health", endpointName: "api")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithVolume("aspireboot-rustfs", "/data")
    .WaitFor(redisResource);

IResourceBuilder<ContainerResource> caddyResource = distributedApplicationBuilder
    .AddContainer("caddy", "caddy", "2.11.7")
    .WithBindMount("./Caddyfile.Development", "/etc/caddy/Caddyfile")
    .WithBindMount("./certs", "/etc/caddy/certs")
    .WithContainerRuntimeArgs("--publish=0.0.0.0:443:443")
    .WaitFor(rustfsResource);

IResourceBuilder<ProjectResource> apiResource = distributedApplicationBuilder
    .AddProject<Projects.AspireBoot_Api>("api")
    .WithEnvironment("AspireBoot__Infrastructure__ObjectStorage__AccessKey", objectStorageAccessKey)
    .WithEnvironment("AspireBoot__Infrastructure__ObjectStorage__SecretKey", objectStorageSecretKeyParameterResource)
    .WithHttpHealthCheck("/health")
    .WithReference(postgresDatabaseResource)
    .WithReference(rabbitResource, "Rabbit")
    .WithReference(redisResource)
    .WaitFor(caddyResource);

IResourceBuilder<ProjectResource> workerResource = distributedApplicationBuilder
    .AddProject<Projects.AspireBoot_Worker>("worker")
    .WithEnvironment("AspireBoot__Infrastructure__ObjectStorage__AccessKey", objectStorageAccessKey)
    .WithEnvironment("AspireBoot__Infrastructure__ObjectStorage__SecretKey", objectStorageSecretKeyParameterResource)
    .WithHttpHealthCheck("/health")
    .WithReference(postgresDatabaseResource)
    .WithReference(rabbitResource, "Rabbit")
    .WithReference(redisResource)
    .WaitFor(apiResource);

distributedApplicationBuilder
    .AddJavaScriptApp("angular", "../AspireBoot.Angular", "start")
    .WithNpm(false)
    .WithHttpsEndpoint(4200, 4200, "https", isProxied: false)
    .WithReference(apiResource)
    .WaitFor(workerResource);

await distributedApplicationBuilder.Build().RunAsync().ConfigureAwait(false);
