namespace AspireBoot.Core.Identity.Sessions;

public record IssuedSession(
    string Id,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool? IsCurrent = null,
    TrackedDetails? TrackedDetails = null);
