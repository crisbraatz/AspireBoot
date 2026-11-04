using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspireBoot.Infrastructure;

public class DataProtectionOptions
{
    [Required] public required string CertificatePassword { get; init; }
    [Required] public required string CertificatePath { get; init; }

    public static DataProtectionOptions Get(
        IServiceCollection serviceCollection, IConfiguration configuration, bool isDevelopment)
    {
        DataProtectionOptions? dataProtectionOptions =
            configuration.GetSection("AspireBoot:Infrastructure:DataProtection").Get<DataProtectionOptions>();
        ArgumentNullException.ThrowIfNull(dataProtectionOptions);

        Validator.ValidateObject(dataProtectionOptions, new ValidationContext(dataProtectionOptions), true);

        serviceCollection.AddSingleton(dataProtectionOptions);

        return dataProtectionOptions;
    }
}
