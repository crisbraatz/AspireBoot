using AspireBoot.ApiService.Validators;
using FluentAssertions;

namespace AspireBoot.Tests.Unit.Api.Validators;

public class PasswordValidatorTests
{
    private const string Password = "abc123-DEF456-ghi789";

    [Fact]
    public void ShouldValidatePasswordFormat()
    {
        ValidatorResponse response = PasswordValidator.ValidateFormat(Password);

        response.Should().NotBeNull();
        response.IsFailure.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("0123456789012345")]
    [InlineData("exampleexampleex")]
    [InlineData("EXAMPLEEXAMPLEEX")]
    [InlineData("ExampleExampleEx")]
    [InlineData("ExampleExample12")]
    [InlineData("01234567890123456789012345678901")]
    [InlineData("exampleexampleexampleexampleexam")]
    [InlineData("EXAMPLEEXAMPLEEXAMPLEEXAMPLEEXAM")]
    [InlineData("ExampleExampleExampleExampleExam")]
    [InlineData("ExampleExampleExampleExample1234")]
    [InlineData("abc123-DEF456-g")]
    [InlineData("abc123-DEF456-ghi789-jkl012-mno34")]
    public void ShouldReturnErrorValidatingInvalidPasswordFormat(string? password)
    {
        ValidatorResponse response = PasswordValidator.ValidateFormat(password);

        response.Should().NotBeNull();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be("Invalid password format.");
        response.IsFailure.Should().BeTrue();
    }
}
