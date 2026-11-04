using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.UpdateUser;

namespace AspireBoot.Tests.Unit.Core.Identity.Users.UpdateUser;

public class UpdateUserCommandValidatorTests
{
    [Fact]
    public void ShouldReturnNoFailuresWhenAcceptingNameUpdate()
    {
        IReadOnlyList<Failure> failures = new UpdateUserCommandValidator().Validate(new UpdateUserCommand("subject", "Updated Name"));

        Assert.Empty(failures);
    }

    [Theory]
    [InlineData("João da Silva")]
    [InlineData("Jose\u0301")]
    public void ShouldReturnNoFailuresWhenAcceptingAccentedNames(string name)
    {
        IReadOnlyList<Failure> failures = new UpdateUserCommandValidator().Validate(new UpdateUserCommand("subject", name));

        Assert.Empty(failures);
    }

    [Fact]
    public void ShouldReturnNoFailuresWhenAcceptingAnAvatarOnlyUpdate()
    {
        IReadOnlyList<Failure> failures = new UpdateUserCommandValidator().Validate(new UpdateUserCommand("account", Avatar: new byte[] { 0xFF, 0xD8, 0xFF }, AvatarContentType: "image/jpeg"));

        Assert.Empty(failures);
    }

    [Fact]
    public void ShouldReturnValidationFailuresAndValidateAvatarContents()
    {
        IReadOnlyList<Failure> failures = new UpdateUserCommandValidator().Validate(new UpdateUserCommand("subject", Avatar: new byte[] { 1, 2, 3 }, AvatarContentType: "image/png"));

        Failure failure = Assert.Single(failures);
        Assert.Equal("identity.users.avatar.invalid", failure.Code);
        Assert.Equal("avatar", failure.Field);
    }

    [Fact]
    public void ShouldReturnValidationFailuresWhenTheUpdateIsEmpty()
    {
        IReadOnlyList<Failure> failures = new UpdateUserCommandValidator().Validate(new UpdateUserCommand("subject"));

        Failure failure = Assert.Single(failures);
        Assert.Equal(FailureKind.Validation, failure.FailureKind);
        Assert.Equal("identity.users.empty", failure.Code);
        Assert.Equal("none", failure.Field);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldReturnValidationFailuresWhenAnIncompleteAvatarUpdateIsProvided(bool missingContentType)
    {
        ReadOnlyMemory<byte>? avatar = null;
        if (missingContentType)
            avatar = new byte[] { 0xFF, 0xD8, 0xFF };

        IReadOnlyList<Failure> failures = new UpdateUserCommandValidator().Validate(new UpdateUserCommand("account", Avatar: avatar, AvatarContentType: missingContentType ? null : "image/jpeg"));

        Assert.Contains(failures, failure => failure.Code is "identity.users.empty");
        Assert.Equal(missingContentType ? 2 : 1, failures.Count);
    }

    [Theory]
    [InlineData("  ")]
    [InlineData(" a ")]
    public void ShouldReturnValidationFailuresWhenNamesThatBecomeTooShortAfterNormalizationAreProvided(string name)
    {
        IReadOnlyList<Failure> failures = new UpdateUserCommandValidator().Validate(new UpdateUserCommand("subject", name));

        Failure failure = Assert.Single(failures);
        Assert.Equal("identity.users.name.length", failure.Code);
        Assert.Equal("newName", failure.Field);
    }

    [Fact]
    public void ShouldReturnValidationFailuresWhenAnOversizedNameIsProvided()
    {
        IReadOnlyList<Failure> failures = new UpdateUserCommandValidator().Validate(new UpdateUserCommand("account", new string('a', 101)));

        Assert.Equal("identity.users.name.length", Assert.Single(failures).Code);
    }

    [Theory]
    [InlineData("User123")]
    [InlineData("123")]
    [InlineData("User-Name")]
    public void ShouldReturnValidationFailuresWhenNamesContainingNonLettersAreProvided(string name)
    {
        IReadOnlyList<Failure> failures = new UpdateUserCommandValidator().Validate(new UpdateUserCommand("subject", name));

        Failure failure = Assert.Single(failures);
        Assert.Equal("identity.users.name.characters", failure.Code);
        Assert.Equal("newName", failure.Field);
    }
}
