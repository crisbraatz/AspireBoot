using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Users.CleanupAvatars;

public class CleanupAvatarsCommandValidator : ICommandValidator<CleanupAvatarsCommand>
{
    public IReadOnlyList<Failure> Validate(CleanupAvatarsCommand command)
    {
        if (command.MinimumAgeInHours is < 24 or > 8760)
            return
            [
                Failure.Validation(
                    "identity.users.avatar.cleanup-minimum-age",
                    "Avatar cleanup minimum age must be between 24 and 8760 hours.",
                    "minimumAgeInHours")
            ];

        if (command.OlderThanUtc > DateTimeOffset.UtcNow.AddHours(-command.MinimumAgeInHours))
            return
            [
                Failure.Validation(
                    "identity.users.avatar.cleanup-age",
                    $"Avatar cleanup requires a grace period of at least {command.MinimumAgeInHours} hours.",
                    "olderThanUtc")
            ];

        return [];
    }
}
