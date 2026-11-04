using System.Security.Claims;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ChangePassword;
using AspireBoot.Core.Identity.Sessions;
using AspireBoot.Core.Identity.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Auth.ChangePassword;

public class ChangePasswordCommandHandlerTests
{
    [Fact]
    public async Task ShouldReturnSuccessWhenChangingThePasswordAndRevokingOtherSessions()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        IUserRepository userRepository = MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name"));
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();
        userRepository.TryChangePasswordAsync(identityUser, "Old-Password123!", "New-Password123!", Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Success));

        Result result = await new ChangePasswordCommandHandler(sessionsTicketStore, signInManager, userManager, userRepository).HandleAsync(new ChangePasswordCommand("Old-Password123!", "New-Password123!", new ClaimsPrincipal()), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await sessionsTicketStore.Received(1).RemoveOthersAsync("account", string.Empty, Arg.Any<CancellationToken>());
        await signInManager.Received(1).SignOutAsync();
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenTheIdentityUserIsUnknown()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        IUserRepository userRepository = MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name"));
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();
        userRepository.TryChangePasswordAsync(identityUser, "Old-Password123!", "New-Password123!", Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Success));
        ChangePasswordCommandHandler commandHandler = new(sessionsTicketStore, signInManager, userManager, userRepository);
        userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(Task.FromResult<IdentityUser?>(null));

        Result result = await commandHandler.HandleAsync(new ChangePasswordCommand("Old-Password123!", "New-Password123!", new ClaimsPrincipal()), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
        await userRepository.DidNotReceive().TryChangePasswordAsync(Arg.Any<IdentityUser>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await sessionsTicketStore.DidNotReceive().RemoveOthersAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await signInManager.DidNotReceive().SignOutAsync();
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenTheUserIsMissing()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        IUserRepository userRepository = MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name"));
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();
        userRepository.TryChangePasswordAsync(identityUser, "Old-Password123!", "New-Password123!", Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Success));
        ChangePasswordCommandHandler commandHandler = new(sessionsTicketStore, signInManager, userManager, userRepository);
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));

        Result result = await commandHandler.HandleAsync(new ChangePasswordCommand("Old-Password123!", "New-Password123!", new ClaimsPrincipal()), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
        await userRepository.DidNotReceive().TryChangePasswordAsync(Arg.Any<IdentityUser>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await sessionsTicketStore.DidNotReceive().RemoveOthersAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await signInManager.DidNotReceive().SignOutAsync();
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenTheCredentialsAreInvalid()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        IUserRepository userRepository = MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name"));
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();
        userRepository.TryChangePasswordAsync(identityUser, "Old-Password123!", "New-Password123!", Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Success));
        ChangePasswordCommandHandler commandHandler = new(sessionsTicketStore, signInManager, userManager, userRepository);
        signInManager.CheckPasswordSignInAsync(identityUser, Arg.Any<string>(), true).Returns(Task.FromResult(SignInResult.Failed));

        Result result = await commandHandler.HandleAsync(new ChangePasswordCommand("Old-Password123!", "New-Password123!", new ClaimsPrincipal()), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
        await userRepository.DidNotReceive().TryChangePasswordAsync(Arg.Any<IdentityUser>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await sessionsTicketStore.DidNotReceive().RemoveOthersAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await signInManager.DidNotReceive().SignOutAsync();
    }

    [Theory]
    [InlineData("PasswordMismatch", "currentPassword")]
    [InlineData("PasswordTooShort", "newPassword")]
    [InlineData("ConcurrencyFailure", "")]
    public async Task ShouldReturnIdentityPasswordFailures(string code, string field)
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        IUserRepository userRepository = MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name"));
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();
        userRepository.TryChangePasswordAsync(identityUser, "Old-Password123!", "New-Password123!", Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Success));
        ChangePasswordCommandHandler commandHandler = new(sessionsTicketStore, signInManager, userManager, userRepository);
        userRepository.TryChangePasswordAsync(identityUser, "Old-Password123!", "New-Password123!", Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = code })));

        Result result = await commandHandler.HandleAsync(new ChangePasswordCommand("Old-Password123!", "New-Password123!", new ClaimsPrincipal()), TestContext.Current.CancellationToken);

        Failure failure = Assert.Single(result.Failures!);
        Assert.Equal(FailureKind.Validation, failure.FailureKind);
        Assert.Equal("identity.auth." + code, failure.Code);
        Assert.Equal(field, failure.Field);
        await sessionsTicketStore.DidNotReceive().RemoveOthersAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await signInManager.DidNotReceive().SignOutAsync();
    }

    private static SignInManager<IdentityUser> MockSignInManager(UserManager<IdentityUser> userManager)
    {
        SignInManager<IdentityUser> signInManager = Substitute.For<SignInManager<IdentityUser>>(userManager, new HttpContextAccessor { HttpContext = new DefaultHttpContext() }, Substitute.For<IUserClaimsPrincipalFactory<IdentityUser>>(), Options.Create(new IdentityOptions()), NullLogger<SignInManager<IdentityUser>>.Instance, Substitute.For<IAuthenticationSchemeProvider>(), Substitute.For<IUserConfirmation<IdentityUser>>());
        signInManager.CheckPasswordSignInAsync(Arg.Any<IdentityUser>(), Arg.Any<string>(), true).Returns(Task.FromResult(SignInResult.Success));
        signInManager.PasswordSignInAsync(Arg.Any<IdentityUser>(), Arg.Any<string>(), Arg.Any<bool>(), true).Returns(Task.FromResult(SignInResult.Success));
        return signInManager;
    }

    private static UserManager<IdentityUser> MockUserManager(IdentityUser? identityUser = null)
    {
        UserManager<IdentityUser> userManager = Substitute.For<UserManager<IdentityUser>>(Substitute.For<IUserStore<IdentityUser>>(), Options.Create(new IdentityOptions()), Substitute.For<IPasswordHasher<IdentityUser>>(), Array.Empty<IUserValidator<IdentityUser>>(), Array.Empty<IPasswordValidator<IdentityUser>>(), Substitute.For<ILookupNormalizer>(), new IdentityErrorDescriber(), Substitute.For<IServiceProvider>(), NullLogger<UserManager<IdentityUser>>.Instance);
        userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(Task.FromResult(identityUser));
        userManager.FindByEmailAsync(Arg.Any<string>()).Returns(Task.FromResult(identityUser));
        userManager.FindByIdAsync(Arg.Any<string>()).Returns(Task.FromResult(identityUser));
        return userManager;
    }

    private static IUserRepository MockUserRepository(User user)
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        return userRepository;
    }
}
