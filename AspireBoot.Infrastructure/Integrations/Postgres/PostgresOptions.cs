using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspireBoot.Infrastructure.Integrations.Postgres;

public class PostgresOptions
{
    public const string SectionName = "AspireBoot:Infrastructure:Database";

    [Required] public required bool? ApplyMigrationsOnStartup { get; init; }

    public static void Add(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        PostgresOptions? postgresOptions = configuration.GetSection(SectionName).Get<PostgresOptions>();
        ArgumentNullException.ThrowIfNull(postgresOptions);

        Validator.ValidateObject(postgresOptions, new ValidationContext(postgresOptions), true);

        serviceCollection.AddSingleton(postgresOptions);
    }
}
