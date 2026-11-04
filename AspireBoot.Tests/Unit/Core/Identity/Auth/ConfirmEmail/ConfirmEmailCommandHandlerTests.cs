using System.Security.Claims;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ConfirmEmail;
using AspireBoot.Core.Identity.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Auth.ConfirmEmail;

public class ConfirmEmailCommandHandlerTests
{
    [Fact]
    public async Task ShouldReturnSuccessWhenConfirmingTheEmail()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        IUserRepository userRepository = MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name"), identityUser);

        Result result = await new ConfirmEmailCommandHandler(userManager, userRepository).HandleAsync(new ConfirmEmailCommand("account", "token"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await userRepository.Received(1).TryConfirmEmailAsync(identityUser, "token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenTheAccountIsUnknown()
    {
        using UserManager<IdentityUser> userManager = MockUserManager();

        Result result = await new ConfirmEmailCommandHandler(userManager, Substitute.For<IUserRepository>()).HandleAsync(new ConfirmEmailCommand("account", "token"), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenTheUserIsMissing()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        IUserRepository userRepository = MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name"), identityUser);
        ConfirmEmailCommandHandler commandHandler = new(userManager, userRepository);
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));

        Result result = await commandHandler.HandleAsync(new ConfirmEmailCommand("account", "token"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
    }

    [Theory]
    [InlineData("InvalidToken", "token")]
    [InlineData("ConcurrencyFailure", "")]
    public async Task ShouldReturnFailureWhenMappingIdentityFailures(string code, string field)
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        IUserRepository userRepository = MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name"), identityUser);
        ConfirmEmailCommandHandler commandHandler = new(userManager, userRepository);
        userRepository.TryConfirmEmailAsync(identityUser, "token", Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = code })));

        Result result = await commandHandler.HandleAsync(new ConfirmEmailCommand("account", "token"), TestContext.Current.CancellationToken);

        Failure failure = Assert.Single(result.Failures!);
        Assert.Equal("identity.auth." + code, failure.Code);
        Assert.Equal(field, failure.Field);
    }

    private static UserManager<IdentityUser> MockUserManager(IdentityUser? identityUser = null)
    {
        UserManager<IdentityUser> userManager = Substitute.For<UserManager<IdentityUser>>(Substitute.For<IUserStore<IdentityUser>>(), Options.Create(new IdentityOptions()), Substitute.For<IPasswordHasher<IdentityUser>>(), Array.Empty<IUserValidator<IdentityUser>>(), Array.Empty<IPasswordValidator<IdentityUser>>(), Substitute.For<ILookupNormalizer>(), new IdentityErrorDescriber(), Substitute.For<IServiceProvider>(), NullLogger<UserManager<IdentityUser>>.Instance);
        userManager.FindByIdAsync(Arg.Any<string>()).Returns(Task.FromResult(identityUser));
        userManager.FindByEmailAsync(Arg.Any<string>()).Returns(Task.FromResult(identityUser));
        userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(Task.FromResult(identityUser));
        return userManager;
    }

    private static IUserRepository MockUserRepository(User user, IdentityUser identityUser)
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        userRepository.TryConfirmEmailAsync(identityUser, "token", Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Success));
        return userRepository;
    }
}
