using AspireBoot.Core.Identity.Users;

namespace AspireBoot.Tests.Unit.Core.Identity.Users;

public class UserTests
{
    [Fact]
    public void ChangeAvatarKeyShouldReturnTrueWhenChangingTheAvatar()
    {
        User user = new("account", "user@domain.com", "User Name");

        bool changed = user.ChangeAvatarKey("users/account/avatar.jpg");

        Assert.True(changed);
        Assert.Equal("users/account/avatar.jpg", user.AvatarKey);
        Assert.NotNull(user.UpdatedAtUtc);
    }

    [Fact]
    public void ChangeAvatarKeyShouldReturnTrueWhenRemovingTheAvatar()
    {
        User user = new("account", "user@domain.com", "User Name");
        user.ChangeAvatarKey("old-avatar");

        bool changed = user.ChangeAvatarKey();

        Assert.True(changed);
        Assert.Null(user.AvatarKey);
    }

    [Fact]
    public void ChangeEmailShouldReturnTrueWhenNormalizingAChangedEmail()
    {
        User user = new("account", "user@domain.com", "User Name");

        bool changed = user.ChangeEmail("NEW@DOMAIN.COM");

        Assert.True(changed);
        Assert.Equal("new@domain.com", user.Email);
        Assert.NotNull(user.UpdatedAtUtc);
    }

    [Fact]
    public void ChangeNameShouldReturnTrueWhenNormalizingAChangedName()
    {
        User user = new("account", "user@domain.com", "User Name");

        bool changed = user.ChangeName("  nEW   nAME  ");

        Assert.True(changed);
        Assert.Equal("New Name", user.Name);
        Assert.NotNull(user.UpdatedAtUtc);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("email")]
    [InlineData("avatar")]
    public void ChangeNameAndChangeEmailAndChangeAvatarKeyShouldReturnFalseWhenTheNormalizedValueDoesNotChange(string field)
    {
        User user = new("account", "user@domain.com", "User Name");
        user.ChangeAvatarKey("old-avatar");
        DateTimeOffset? previous = user.UpdatedAtUtc;

        bool changed = field switch { "name" => user.ChangeName("  USER NAME  "), "email" => user.ChangeEmail("USER@DOMAIN.COM"), _ => user.ChangeAvatarKey("old-avatar") };

        Assert.False(changed);
        Assert.Equal(previous, user.UpdatedAtUtc);
    }

    [Fact]
    public void ConstructorShouldInitializeANormalizedProfile()
    {
        User user = new("account", "USER@DOMAIN.COM", "  uSER\t nAME  ");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("account", user.IdentityUserId);
        Assert.Equal("user@domain.com", user.Email);
        Assert.Equal("User Name", user.Name);
        Assert.Null(user.AvatarKey);
        Assert.Null(user.UpdatedAtUtc);
    }

    [Fact]
    public void DeactivateShouldDeactivateTheUserAndMarkTheProfileAsChanged()
    {
        User user = new("account", "user@example.com", "User Name");

        user.Deactivate();

        Assert.False(user.IsActive);
        Assert.NotNull(user.UpdatedAtUtc);
        Assert.Equal("account", user.IdentityUserId);
    }
}
