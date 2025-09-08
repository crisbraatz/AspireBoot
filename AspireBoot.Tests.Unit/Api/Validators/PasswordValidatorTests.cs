using AspireBoot.Api.Validators;
using FluentAssertions;

namespace AspireBoot.Tests.Unit.Api.Validators;

public class PasswordValidatorTests
{
    private const string Password = "Example123";

    [Fact]
    public void Should_validate_password_format()
    {
        var response = PasswordValidator.ValidateFormat(Password);

        response.Should().NotBeNull();
        response.IsFailure.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Example")]
    [InlineData("Example123Example123")]
    [InlineData("example123")]
    [InlineData("EXAMPLE123")]
    [InlineData("ExampleE")]
    [InlineData("12312312")]
    [InlineData("Example123!")]
    public void Should_return_error_validating_invalid_password_format(string? password)
    {
        var response = PasswordValidator.ValidateFormat(password);

        response.Should().NotBeNull();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be("Invalid password format.");
        response.IsFailure.Should().BeTrue();
    }
}