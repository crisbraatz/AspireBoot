namespace AspireBoot.Core.Identity;

public static class IdentityConstants
{
    public const string AdministratorRole = "Administrator";
    public const string SessionCreatedAtProperty = "session-created-at";
    public const string SessionIdProperty = "session-id";
    public const string SessionsCachePrefix = "identity:sessions:";
    public const string SessionTrackedDetailsProperty = "session-tracked-details";
    public const string UsersCachePrefix = "identity:users:";

    public static string GetAuthSessionsKey(string identityUserId) => $"identity:auth:{identityUserId}:sessions";

# pragma warning disable CA1308
    public static string NormalizeEmail(string email) => email.ToLowerInvariant();
#pragma warning restore CA1308

    public static string NormalizeName(string name) =>
        string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
#pragma warning disable CA1308
            .Select(word => string.Concat(word[..1].ToUpperInvariant(), word[1..].ToLowerInvariant())));
#pragma warning restore CA1308
}
