using AspireBoot.ApiService.Validators;
using FluentAssertions;

namespace AspireBoot.Tests.Unit.Api.Validators;

public class EmailValidatorTests
{
    [Theory]
    [InlineData("example@email.com")]
    [InlineData("example@email.com.br")]
    [InlineData("example.example@email.com")]
    [InlineData("example.example@email.com.br")]
    [InlineData("example_example@email.com")]
    [InlineData("example_example@email.com.br")]
    public void ShouldValidateEmailFormat(string email)
    {
        ValidatorResponse response = EmailValidator.ValidateFormat(email);

        response.Should().NotBeNull();
        response.IsFailure.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".example@email.com")]
    [InlineData("example@email.com.")]
    [InlineData("example@email..com")]
    [InlineData("example@email.c")]
    [InlineData("example@email.company")]
    public void ShouldReturnErrorValidatingInvalidEmailFormat(string? email)
    {
        ValidatorResponse response = EmailValidator.ValidateFormat(email);

        response.Should().NotBeNull();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be("Invalid email format.");
        response.IsFailure.Should().BeTrue();
    }
}
