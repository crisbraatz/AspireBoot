using AspireBoot.Infrastructure.Integrations.Rabbit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Wolverine.EntityFrameworkCore;

namespace AspireBoot.Infrastructure.Integrations.Postgres;

public static class PostgresDependencyInjection
{
    public static void AddPostgres(
        this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("Postgres");
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        PostgresOptions.Add(serviceCollection, configuration);

        serviceCollection.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).EnableDynamicJson().Build());
        serviceCollection.AddDbContextWithWolverineIntegration<AspireBootDbContext>(
            (serviceProvider, dbContextOptionsBuilder) =>
                dbContextOptionsBuilder.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>()),
            RabbitDependencyInjection.WolverineDatabaseSchema);
        serviceCollection.AddHealthChecks()
            .AddDbContextCheck<AspireBootDbContext>(
                "postgres-dbcontext", HealthStatus.Unhealthy, ["ready", "dependency", "postgres"])
            .AddNpgSql(
                connectionString,
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "dependency", "postgres"]);
    }
}
