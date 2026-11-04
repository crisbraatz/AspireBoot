using AspireBoot.Core;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.Register;
using AspireBoot.Core.Identity.Auth.SendEmailConfirmation;
using AspireBoot.Core.Identity.Users;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Auth.Register;

public class RegisterCommandHandlerTests
{
    [Fact]
    public async Task ShouldReturnSuccessWhenCreatingANormalizedAccountAndProfile()
    {
        IUserRepository userRepository = MockUserRepository();

        Result<RegisterResult> result = await new RegisterCommandHandler(userRepository).HandleAsync(new RegisterCommand("USER@EXAMPLE.COM", "Test-Password123!", "  uSER   nAME ", "https://app.tests"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(!CoreConstants.IsDevelopment, result.Value!.RequiresEmailConfirmation);
        await userRepository.Received(1).TryAddAsync(Arg.Is<User>(user => user.Name == "User Name" && user.Email == "user@example.com"), Arg.Any<IdentityUser>(), "Test-Password123!", Arg.Is<SendEmailConfirmationEvent?>(sendEmailConfirmationEvent => CoreConstants.IsDevelopment ? sendEmailConfirmationEvent == null : sendEmailConfirmationEvent != null && sendEmailConfirmationEvent.Email == "user@example.com"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenTheEmailAlreadyExists()
    {
        IUserRepository userRepository = MockUserRepository();
        RegisterCommandHandler commandHandler = new(userRepository);
        userRepository.TryAddAsync(Arg.Any<User>(), Arg.Any<IdentityUser>(), Arg.Any<string>(), Arg.Any<SendEmailConfirmationEvent?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail" })));

        Result<RegisterResult> result = await commandHandler.HandleAsync(new RegisterCommand("user@example.com", "Test-Password123!", "User Name", "https://app.tests"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("InvalidEmail", "email")]
    [InlineData("PasswordTooShort", "password")]
    [InlineData("ConcurrencyFailure", "")]
    public async Task ShouldReturnFailureWhenMappingNonDuplicateIdentityFailures(string code, string field)
    {
        IUserRepository userRepository = MockUserRepository();
        RegisterCommandHandler commandHandler = new(userRepository);
        userRepository.TryAddAsync(Arg.Any<User>(), Arg.Any<IdentityUser>(), Arg.Any<string>(), Arg.Any<SendEmailConfirmationEvent?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = code })));

        Result<RegisterResult> result = await commandHandler.HandleAsync(new RegisterCommand("USER@EXAMPLE.COM", "Test-Password123!", "  uSER   nAME ", "https://app.tests"), TestContext.Current.CancellationToken);

        Failure failure = Assert.Single(result.Failures!);
        Assert.Equal("identity.auth." + code, failure.Code);
        Assert.Equal(field, failure.Field);
    }

    [Fact]
    public async Task ShouldReturnFailureWhenIgnoreOnlyTheDuplicateEmailError()
    {
        IUserRepository userRepository = MockUserRepository();
        RegisterCommandHandler commandHandler = new(userRepository);
        userRepository.TryAddAsync(Arg.Any<User>(), Arg.Any<IdentityUser>(), Arg.Any<string>(), Arg.Any<SendEmailConfirmationEvent?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail" }, new IdentityError { Code = "PasswordTooShort" })));

        Result<RegisterResult> result = await commandHandler.HandleAsync(new RegisterCommand("USER@EXAMPLE.COM", "Test-Password123!", "  uSER   nAME ", "https://app.tests"), TestContext.Current.CancellationToken);

        Failure failure = Assert.Single(result.Failures!);
        Assert.Equal("identity.auth.PasswordTooShort", failure.Code);
        Assert.Equal("password", failure.Field);
    }

    private static IUserRepository MockUserRepository()
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.TryAddAsync(Arg.Any<User>(), Arg.Any<IdentityUser>(), Arg.Any<string>(), Arg.Any<SendEmailConfirmationEvent?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Success));
        return userRepository;
    }
}
