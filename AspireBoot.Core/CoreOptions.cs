using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspireBoot.Core;

public class CoreOptions
{
    [Required] public required EmailRequestsOptions EmailRequests { get; init; }

    public static void Add(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        CoreOptions? coreOptions = configuration.GetSection("AspireBoot:Core").Get<CoreOptions>();
        ArgumentNullException.ThrowIfNull(coreOptions);

        Validator.ValidateObject(coreOptions, new ValidationContext(coreOptions), true);
        Validator.ValidateObject(coreOptions.EmailRequests, new ValidationContext(coreOptions.EmailRequests), true);

        if (coreOptions.EmailRequests.CooldownInSeconds >
            TimeSpan.FromMinutes(coreOptions.EmailRequests.WindowInMinutes).TotalSeconds)
            throw new ValidationException("EmailRequests CooldownInSeconds must not exceed WindowInMinutes.");

        serviceCollection.AddSingleton(coreOptions);
        serviceCollection.AddSingleton(coreOptions.EmailRequests);
    }
}

public class EmailRequestsOptions
{
    [Required] [Range(1, 3600)] public required int CooldownInSeconds { get; init; }
    [Required] [Range(1, 1000)] public required int PermitLimit { get; init; }
    [Required] [Range(1, 60)] public required int WindowInMinutes { get; init; }
}
