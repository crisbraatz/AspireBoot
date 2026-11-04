using System.Security.Claims;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Identity.Users.DeleteUser;
using AspireBoot.Core.Integrations.Caching;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Users.DeleteUser;

public class DeleteUserCommandHandlerTests
{
    [Fact]
    public async Task ShouldReturnSuccessWhenDeletingTheOwnedAccountAndRevokingItsSessions()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        User user = new(identityUser.Id, identityUser.Email, "User Name");
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();

        Result result = await new DeleteUserCommandHandler(Substitute.For<ICache>(), sessionsTicketStore, signInManager, userManager, MockUserRepository(user, identityUser)).HandleAsync(new DeleteUserCommand(user.Id, "account", "Test-Password123!"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await sessionsTicketStore.Received(1).RemoveOthersAsync("account", string.Empty, Arg.Any<CancellationToken>());
        await signInManager.Received(1).SignOutAsync();
    }

    [Fact]
    public async Task ShouldReturnNotFoundFailureWhenTheUserIsMissing()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        User user = new(identityUser.Id, identityUser.Email, "User Name");
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        IUserRepository userRepository = MockUserRepository(user, identityUser);
        ICache cache = Substitute.For<ICache>();
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();
        DeleteUserCommandHandler commandHandler = new(cache, sessionsTicketStore, signInManager, userManager, userRepository);
        userRepository.FindByIdAsync(user.Id, false, false, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));

        Result result = await commandHandler.HandleAsync(new DeleteUserCommand(user.Id, "account", "Test-Password123!"), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.NotFound, Assert.Single(result.Failures!).FailureKind);
        await sessionsTicketStore.DidNotReceive().RemoveOthersAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await signInManager.DidNotReceive().SignOutAsync();
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenTheIdentityUserIsUnknown()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        User user = new(identityUser.Id, identityUser.Email, "User Name");
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        ICache cache = Substitute.For<ICache>();
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();
        DeleteUserCommandHandler commandHandler = new(cache, sessionsTicketStore, signInManager, userManager, MockUserRepository(user, identityUser));
        userManager.FindByIdAsync("account").Returns(Task.FromResult<IdentityUser?>(null));

        Result result = await commandHandler.HandleAsync(new DeleteUserCommand(user.Id, "account", "Test-Password123!"), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
        await sessionsTicketStore.DidNotReceive().RemoveOthersAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await signInManager.DidNotReceive().SignOutAsync();
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenAnAccountOwnershipMismatchIsProvided()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        User user = new(identityUser.Id, identityUser.Email, "User Name");
        using UserManager<IdentityUser> userManager = MockUserManager(new IdentityUser { Id = "another-account" });
        IUserRepository userRepository = MockUserRepository(user, identityUser);

        Result result = await new DeleteUserCommandHandler(Substitute.For<ICache>(), Substitute.For<ISessionsTicketStore>(), MockSignInManager(userManager), userManager, userRepository).HandleAsync(new DeleteUserCommand(user.Id, "another-account", "Test-Password123!"), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
        await userRepository.DidNotReceive().DeleteAsync(Arg.Any<User>(), Arg.Any<IdentityUser>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenThePasswordIsIncorrect()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        User user = new(identityUser.Id, identityUser.Email, "User Name");
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        ICache cache = Substitute.For<ICache>();
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();
        DeleteUserCommandHandler commandHandler = new(cache, sessionsTicketStore, signInManager, userManager, MockUserRepository(user, identityUser));
        signInManager.CheckPasswordSignInAsync(identityUser, Arg.Any<string>(), true).Returns(Task.FromResult(SignInResult.Failed));

        Result result = await commandHandler.HandleAsync(new DeleteUserCommand(user.Id, "account", "Test-Password123!"), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
        await sessionsTicketStore.DidNotReceive().RemoveOthersAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await signInManager.DidNotReceive().SignOutAsync();
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnConflictFailureWhenPreservingTheAccountAfterADeletionConflict()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        User user = new(identityUser.Id, identityUser.Email, "User Name");
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        IUserRepository userRepository = MockUserRepository(user, identityUser);
        ICache cache = Substitute.For<ICache>();
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();
        DeleteUserCommandHandler commandHandler = new(cache, sessionsTicketStore, signInManager, userManager, userRepository);
        userRepository.DeleteAsync(user, identityUser, Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure" })));

        Result result = await commandHandler.HandleAsync(new DeleteUserCommand(user.Id, "account", "Test-Password123!"), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.Conflict, Assert.Single(result.Failures!).FailureKind);
        await sessionsTicketStore.DidNotReceive().RemoveOthersAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await signInManager.DidNotReceive().SignOutAsync();
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
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
        userManager.FindByIdAsync(Arg.Any<string>()).Returns(Task.FromResult(identityUser));
        userManager.FindByEmailAsync(Arg.Any<string>()).Returns(Task.FromResult(identityUser));
        userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(Task.FromResult(identityUser));
        return userManager;
    }

    private static IUserRepository MockUserRepository(User user, IdentityUser identityUser)
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        userRepository.FindByIdAsync(user.Id, false, false, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        userRepository.DeleteAsync(user, identityUser, Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Success));
        return userRepository;
    }
}
