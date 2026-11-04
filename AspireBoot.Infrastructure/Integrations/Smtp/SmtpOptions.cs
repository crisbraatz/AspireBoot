using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspireBoot.Infrastructure.Integrations.Smtp;

public class SmtpOptions
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public required string From { get; init; }

    [Required] public required string Host { get; init; }
    [Required] public required string Password { get; init; }
    [Range(1, 65535)] public required int Port { get; init; }
    [Required] public required bool StartTls { get; init; }
    [Required] public required string Username { get; init; }

    public static void Add(IServiceCollection serviceCollection, IConfiguration configuration, bool isDevelopment)
    {
        SmtpOptions? smtpOptions = configuration.GetSection("AspireBoot:Infrastructure:Smtp").Get<SmtpOptions>();
        ArgumentNullException.ThrowIfNull(smtpOptions);

        Validator.ValidateObject(smtpOptions, new ValidationContext(smtpOptions), true);

        if (!isDevelopment && !smtpOptions.StartTls)
            throw new InvalidOperationException("Smtp StartTls must be enabled outside Development.");

        serviceCollection.AddSingleton(smtpOptions);
    }
}
