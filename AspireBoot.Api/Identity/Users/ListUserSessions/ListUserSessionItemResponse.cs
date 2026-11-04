namespace AspireBoot.Api.Identity.Users.ListUserSessions;

public record ListUserSessionItemResponse(
    string Id,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string Browser,
    string OperatingSystem,
    string Device,
    string? RemoteIpAddress = null,
    bool? IsCurrent = null);
