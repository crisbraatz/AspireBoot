using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.CleanupAvatars;

namespace AspireBoot.Tests.Unit.Core.Identity.Users.CleanupAvatars;

public class CleanupAvatarsCommandValidatorTests
{
    [Fact]
    public void ShouldReturnNoFailuresWhenAcceptingAnOldEnoughCutoff()
    {
        IReadOnlyList<Failure> failures = new CleanupAvatarsCommandValidator().Validate(new CleanupAvatarsCommand(DateTimeOffset.UtcNow.AddHours(-49), 48));

        Assert.Empty(failures);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(23)]
    [InlineData(8761)]
    public void ShouldReturnValidationFailuresWhenAnUnsafeMinimumAgeIsProvided(int hours)
    {
        IReadOnlyList<Failure> failures = new CleanupAvatarsCommandValidator().Validate(new CleanupAvatarsCommand(DateTimeOffset.UtcNow.AddDays(-10), hours));

        Assert.Equal("minimumAgeInHours", Assert.Single(failures).Field);
    }

    [Fact]
    public void ShouldReturnValidationFailuresWhenACutoffInsideTheConfiguredGracePeriodIsProvided()
    {
        IReadOnlyList<Failure> failures = new CleanupAvatarsCommandValidator().Validate(new CleanupAvatarsCommand(DateTimeOffset.UtcNow.AddHours(-30), 48));

        Assert.Equal("identity.users.avatar.cleanup-age", Assert.Single(failures).Code);
    }
}
