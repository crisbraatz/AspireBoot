using AspireBoot.Api.Validators;
using FluentAssertions;

namespace AspireBoot.Tests.Unit.Api.Validators;

public class EmailValidatorTests
{
    [Theory]
    [InlineData("example@email.com")]
    [InlineData("example@email.com.br")]
    [InlineData("example@email.com.br.sc")]
    [InlineData("example.example@email.com")]
    [InlineData("example_example@email.com")]
    public void Should_validate_email_format(string email)
    {
        var response = EmailValidator.ValidateFormat(email);

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
    public void Should_return_error_validating_invalid_email_format(string? email)
    {
        var response = EmailValidator.ValidateFormat(email);

        response.Should().NotBeNull();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be("Invalid email format.");
        response.IsFailure.Should().BeTrue();
    }
}