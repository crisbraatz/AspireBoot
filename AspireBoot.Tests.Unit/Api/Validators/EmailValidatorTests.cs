using AspireBoot.Api.Validators;
using FluentAssertions;

namespace AspireBoot.Tests.Unit.Api.Validators;

public class EmailValidatorTests
{
    [Theory]
    [InlineData("example@template.com")]
    [InlineData("example@template.com.br")]
    [InlineData("example@template.com.br.sc")]
    [InlineData("example.example@template.com")]
    [InlineData("example_example@template.com")]
    public void Should_validate_email_format(string email)
    {
        var response = EmailValidator.ValidateFormat(email);

        response.Should().NotBeNull();
        response.IsFailure.Should().BeFalse();
    }

    [Theory]
    [InlineData(".example@template.com")]
    [InlineData("example@template.com.")]
    [InlineData("example@template..com")]
    [InlineData("example@template.c")]
    [InlineData("example@template.company")]
    public void Should_throw_exception_validating_invalid_email_format(string email)
    {
        var response = EmailValidator.ValidateFormat(email);

        response.Should().NotBeNull();
        response.ErrorCode.Should().Be(400);
        response.ErrorMessage.Should().Be("Invalid email format.");
        response.IsFailure.Should().BeTrue();
    }
}