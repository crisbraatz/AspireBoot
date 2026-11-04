using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.Register;

namespace AspireBoot.Tests.Unit.Core.Identity.Auth.Register;

public class RegisterCommandValidatorTests
{
    [Fact]
    public void ShouldReturnNoFailuresWhenAcceptingNamesAtTheNormalizedLengthLimit()
    {
        IReadOnlyList<Failure> failures = new RegisterCommandValidator().Validate(new RegisterCommand("user@domain.com", "Test-Password123!", "  " + new string('a', 100) + "  ", "https://app.tests"));

        Assert.Empty(failures);
    }

    [Theory]
    [InlineData("  ")]
    [InlineData(" a ")]
    public void ShouldReturnValidationFailuresWhenNamesThatBecomeTooShortAfterNormalizationAreProvided(string name)
    {
        IReadOnlyList<Failure> failures = new RegisterCommandValidator().Validate(new RegisterCommand("user@domain.com", "Test-Password123!", name, "https://app.tests"));

        Failure failure = Assert.Single(failures);
        Assert.Equal("identity.auth.name.length", failure.Code);
        Assert.Equal("name", failure.Field);
    }

    [Fact]
    public void ShouldReturnValidationFailuresWhenNamesLongerThanTheNormalizedLimitAreProvided()
    {
        IReadOnlyList<Failure> failures = new RegisterCommandValidator().Validate(new RegisterCommand("user@domain.com", "Test-Password123!", new string('a', 101), "https://app.tests"));

        Assert.Equal("identity.auth.name.length", Assert.Single(failures).Code);
    }

    [Theory]
    [InlineData("User123")]
    [InlineData("User_Name")]
    public void ShouldReturnValidationFailuresWhenNonLettersAfterValidatingNameLengthAreProvided(string name)
    {
        IReadOnlyList<Failure> failures = new RegisterCommandValidator().Validate(new RegisterCommand("user@domain.com", "Test-Password123!", name, "https://app.tests"));

        Assert.Equal("identity.auth.name.characters", Assert.Single(failures).Code);
    }
}
