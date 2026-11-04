using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Identity.Users.Audit;
using AspireBoot.Core.Identity.Users.CleanupAvatars;
using AspireBoot.Core.Integrations.Messaging;
using AspireBoot.Infrastructure.Identity;
using AspireBoot.Infrastructure.Identity.Users;
using AspireBoot.Infrastructure.Identity.Users.Audit;
using AspireBoot.Infrastructure.Identity.Users.CleanupAvatars;
using AspireBoot.Infrastructure.Integrations.Postgres;
using AspireBoot.Infrastructure.Integrations.Rabbit;
using AspireBoot.Infrastructure.Integrations.Redis;
using AspireBoot.Infrastructure.Integrations.Rustfs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspireBoot.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static void AddApiInfrastructure(
        this IServiceCollection serviceCollection, IConfiguration configuration, bool isDevelopment)
    {
        serviceCollection.AddPostgres(configuration);
        serviceCollection.AddApiRedis(configuration);
        serviceCollection.AddRustfs(configuration, isDevelopment);
        serviceCollection.AddRepositories();
    }

    public static void AddWorkerInfrastructure(
        this IServiceCollection serviceCollection, IConfiguration configuration, bool isDevelopment)
    {
        serviceCollection.AddPostgres(configuration);
        serviceCollection.AddWorkerRedis(configuration);
        serviceCollection.AddRustfs(configuration, isDevelopment);
        serviceCollection.AddInfrastructureIdentity(configuration, isDevelopment);
        serviceCollection.AddRepositories();
    }

    private static void AddRepositories(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IEventRepository, RabbitEventRepository>();
        serviceCollection.AddScoped<ICleanupAvatarRepository, CleanupAvatarRepository>();
        serviceCollection.AddScoped<IUserAuditRepository, UserAuditRepository>();
        serviceCollection.AddScoped<IUserRepository, UserRepository>();
    }
}
