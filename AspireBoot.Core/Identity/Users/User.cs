using AspireBoot.Core.Primitives;

namespace AspireBoot.Core.Identity.Users;

public class User(string identityUserId, string email, string name) : Entity
{
    public string IdentityUserId { get; private set; } = identityUserId;
    public string Email { get; private set; } = IdentityConstants.NormalizeEmail(email);
    public string Name { get; private set; } = IdentityConstants.NormalizeName(name);
    public string? AvatarKey { get; private set; }
    public bool IsActive { get; private set; } = true;

    public bool ChangeAvatarKey(string? avatarKey = null)
    {
        if (string.Equals(AvatarKey, avatarKey, StringComparison.Ordinal))
            return false;

        AvatarKey = avatarKey;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        return true;
    }

    public bool ChangeEmail(string email)
    {
        email = IdentityConstants.NormalizeEmail(email);

        if (Email.Equals(email, StringComparison.Ordinal))
            return false;

        Email = email;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        return true;
    }

    public bool ChangeName(string name)
    {
        name = IdentityConstants.NormalizeName(name);

        if (Name.Equals(name, StringComparison.Ordinal))
            return false;

        Name = name;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        return true;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
