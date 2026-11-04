using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspireBoot.Infrastructure.Integrations.Rustfs;

public class RustfsOptions
{
    [Required]
    [MinLength(3)]
    [MaxLength(256)]
    public required string AccessKey { get; init; }

    [Required]
    [RegularExpression("^[a-z0-9][a-z0-9.-]{1,61}[a-z0-9]$")]
    public required string BucketName { get; init; }

    [Required] public required bool CreateBucketIfMissing { get; init; }
    [Required] [Url] public required string Endpoint { get; init; }
    [Required] [Url] public required string PublicEndpoint { get; init; }
    [Required] public required string ReadinessPath { get; init; }
    [Required] public required string Region { get; init; }

    [Required]
    [MinLength(8)]
    [MaxLength(256)]
    public required string SecretKey { get; init; }

    [Required] [Range(1, 7)] public required int UrlExpiresInDays { get; init; }

    public static RustfsOptions Get(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        RustfsOptions? rustfsOptions =
            configuration.GetSection("AspireBoot:Infrastructure:ObjectStorage").Get<RustfsOptions>();
        ArgumentNullException.ThrowIfNull(rustfsOptions);

        Validator.ValidateObject(rustfsOptions, new ValidationContext(rustfsOptions), true);

        serviceCollection.AddSingleton(rustfsOptions);

        return rustfsOptions;
    }
}
