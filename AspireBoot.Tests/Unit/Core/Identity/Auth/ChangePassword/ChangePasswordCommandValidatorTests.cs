using System.Security.Claims;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ChangePassword;

namespace AspireBoot.Tests.Unit.Core.Identity.Auth.ChangePassword;

public class ChangePasswordCommandValidatorTests
{
    [Fact]
    public void ShouldReturnNoFailuresWhenAcceptingADifferentPassword()
    {
        IReadOnlyList<Failure> failures = new ChangePasswordCommandValidator().Validate(new ChangePasswordCommand("Old-Password123!", "New-Password123!", new ClaimsPrincipal()));

        Assert.Empty(failures);
    }

    [Fact]
    public void ShouldReturnValidationFailuresWhenThePasswordIsUnchanged()
    {
        IReadOnlyList<Failure> failures = new ChangePasswordCommandValidator().Validate(new ChangePasswordCommand("Same-Password123!", "Same-Password123!", new ClaimsPrincipal()));

        Failure failure = Assert.Single(failures);
        Assert.Equal("identity.auth.password-unchanged", failure.Code);
        Assert.Equal("newPassword", failure.Field);
    }
}
