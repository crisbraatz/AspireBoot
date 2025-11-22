using AspireBoot.ApiService.Helpers;
using FluentAssertions;

namespace AspireBoot.Tests.Unit.Api.Helpers;

public class PasswordHelperTests
{
    private const string User = "example@email.com";
    private const string Password = "abc123-DEF456-ghi789";

    [Fact]
    public void ShouldGetHashedPassword()
    {
        string hashedPassword = User.GetHashedPassword(Password);

        hashedPassword.Should().NotBe(Password);
    }

    [Theory]
    [InlineData(Password, true)]
    [InlineData("StrongPassword123!", false)]
    public void ShouldAssertMatch(string providedPassword, bool expectedMatch)
    {
        bool returnedMatch = User.IsMatch(User.GetHashedPassword(Password), providedPassword);

        returnedMatch.Should().Be(expectedMatch);
    }
}
