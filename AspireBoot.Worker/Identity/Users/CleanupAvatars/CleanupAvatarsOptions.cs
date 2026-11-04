using System.ComponentModel.DataAnnotations;

namespace AspireBoot.Worker.Identity.Users.CleanupAvatars;

public class CleanupAvatarsOptions
{
    [Range(1, 168)] public required int IntervalInHours { get; init; }
    [Range(24, 8760)] public required int MinimumAgeInHours { get; init; }

    public static void Add(IServiceCollection services, IConfiguration configuration)
    {
        CleanupAvatarsOptions? cleanupAvatarsOptions =
            configuration.GetSection("AspireBoot:Worker:CleanupAvatars").Get<CleanupAvatarsOptions>();
        ArgumentNullException.ThrowIfNull(cleanupAvatarsOptions);

        Validator.ValidateObject(cleanupAvatarsOptions, new ValidationContext(cleanupAvatarsOptions), true);

        services.AddSingleton(cleanupAvatarsOptions);
    }
}
