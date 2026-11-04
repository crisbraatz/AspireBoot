using System.ComponentModel.DataAnnotations;

namespace AspireBoot.Api;

public class ApiOptions
{
    [Required] public required FrontendOptions Frontend { get; init; }
    [Required] public required RateLimitersOptions RateLimiters { get; init; }
    [Required] public required ReverseProxyOptions ReverseProxy { get; init; }

    public static ApiOptions Get(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        ApiOptions? apiOptions = configuration.GetSection("AspireBoot:Api").Get<ApiOptions>();
        ArgumentNullException.ThrowIfNull(apiOptions);

        Validator.ValidateObject(apiOptions, new ValidationContext(apiOptions), true);
        FrontendOptions.Validate(apiOptions.Frontend);
        Validator.ValidateObject(apiOptions.RateLimiters, new ValidationContext(apiOptions.RateLimiters), true);
        Validator.ValidateObject(
            apiOptions.RateLimiters.Auth, new ValidationContext(apiOptions.RateLimiters.Auth), true);
        Validator.ValidateObject(
            apiOptions.RateLimiters.Reads, new ValidationContext(apiOptions.RateLimiters.Reads), true);
        Validator.ValidateObject(
            apiOptions.RateLimiters.Writes, new ValidationContext(apiOptions.RateLimiters.Writes), true);

        serviceCollection.AddSingleton(apiOptions);
        serviceCollection.AddSingleton(apiOptions.Frontend);
        serviceCollection.AddSingleton(apiOptions.RateLimiters);
        serviceCollection.AddSingleton(apiOptions.ReverseProxy);

        return apiOptions;
    }
}

public class FrontendOptions
{
    [Required]
    [Url]
    [MinLength(1)]
    [MaxLength(2048)]
    public required string Origin { get; init; }

    public static void Validate(FrontendOptions frontendOptions)
    {
        if (!Uri.TryCreate(frontendOptions.Origin, UriKind.Absolute, out Uri? origin) ||
            origin.Scheme is not "https" ||
            origin.AbsolutePath is not "/" ||
            !string.IsNullOrEmpty(origin.UserInfo) ||
            !string.IsNullOrEmpty(origin.Query) ||
            !string.IsNullOrEmpty(origin.Fragment))
            throw new InvalidOperationException(
                "Frontend Origin must be an HTTPS origin without path, credentials, query or fragment.");

        Validator.ValidateObject(frontendOptions, new ValidationContext(frontendOptions), true);
    }
}

public class RateLimiterOptions
{
    [Required] [Range(1, 1000)] public required int PermitLimit { get; init; }
    [Required] [Range(1, 60)] public required int WindowInMinutes { get; init; }
}

public class RateLimitersOptions
{
    [Required] public required RateLimiterOptions Auth { get; init; }
    [Required] public required RateLimiterOptions Reads { get; init; }
    [Required] public required RateLimiterOptions Writes { get; init; }
}

public class ReverseProxyOptions
{
    [Required]
    [MinLength(4)]
    [MaxLength(49)]
    public required string IpNetwork { get; init; }
}
