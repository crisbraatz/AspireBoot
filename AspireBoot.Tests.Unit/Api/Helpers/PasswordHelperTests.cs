using AspireBoot.Api.Helpers;
using FluentAssertions;

namespace AspireBoot.Tests.Unit.Api.Helpers;

public class PasswordHelperTests
{
    private const string User = "example@template.com";
    private const string Password = "Example123";

    [Fact]
    public void Should_get_hashed_password()
    {
        var hashedPassword = User.GetHashedPassword(Password);

        hashedPassword.Should().NotBe(Password);
    }

    [Theory]
    [InlineData(Password, true)]
    [InlineData("Example1234", false)]
    public void Should_assert_match(string providedPassword, bool expectedMatch)
    {
        var returnedMatch = User.IsMatch(User.GetHashedPassword(Password), providedPassword);

        returnedMatch.Should().Be(expectedMatch);
    }
}