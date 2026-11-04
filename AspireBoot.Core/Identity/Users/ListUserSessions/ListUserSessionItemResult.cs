using AspireBoot.Core.Identity.Sessions;

namespace AspireBoot.Core.Identity.Users.ListUserSessions;

public record ListUserSessionItemResult(
    string Id,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    TrackedDetails? TrackedDetails = null,
    bool? IsCurrent = null);
