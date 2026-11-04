using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Users.CleanupAvatars;

public record CleanupAvatarsCommand(DateTimeOffset OlderThanUtc, int MinimumAgeInHours)
    : ICommand<CleanupAvatarsResult>;
