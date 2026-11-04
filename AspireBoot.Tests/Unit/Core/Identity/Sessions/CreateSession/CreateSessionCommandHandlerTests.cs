using System.Security.Claims;
using AspireBoot.Core.Cqrs;
using IdentityConstants = AspireBoot.Core.Identity.IdentityConstants;
using AspireBoot.Core.Identity.Sessions.CreateSession;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Messaging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Sessions.CreateSession;

public class CreateSessionCommandHandlerTests
{
    [Fact]
    public async Task ShouldReturnSuccessWhenIssuingASessionForAnActiveConfirmedAccount()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        IEventRepository eventRepository = Substitute.For<IEventRepository>();

        Result result = await new CreateSessionCommandHandler(eventRepository, MockSignInManager(userManager), userManager, MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name"))).HandleAsync(new CreateSessionCommand("user@example.com", "Test-Password123!", false, "Browser", "127.0.0.1"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await eventRepository.Received(1).AddAsync(Arg.Any<CreateSessionEvent>(), true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenTheIdentityUserIsUnknown()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        CreateSessionCommandHandler commandHandler = new(eventRepository, signInManager, userManager, MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name")));
        userManager.FindByEmailAsync(Arg.Any<string>()).Returns(Task.FromResult<IdentityUser?>(null));

        Result result = await commandHandler.HandleAsync(new CreateSessionCommand("user@example.com", "Test-Password123!", false, "Browser", "127.0.0.1"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
        await signInManager.DidNotReceive().PasswordSignInAsync(Arg.Any<IdentityUser>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>());
        await eventRepository.DidNotReceive().AddAsync(Arg.Any<IEvent>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenTheUserIsMissing()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        IUserRepository userRepository = MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name"));
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        CreateSessionCommandHandler commandHandler = new(eventRepository, signInManager, userManager, userRepository);
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));

        Result result = await commandHandler.HandleAsync(new CreateSessionCommand("user@example.com", "Test-Password123!", false, "Browser", "127.0.0.1"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
        await signInManager.DidNotReceive().PasswordSignInAsync(Arg.Any<IdentityUser>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>());
        await eventRepository.DidNotReceive().AddAsync(Arg.Any<IEvent>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenTheUserIsInactive()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        User user = new(identityUser.Id, identityUser.Email, "User Name");
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        IUserRepository userRepository = MockUserRepository(user);
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        user.Deactivate();

        Result result = await new CreateSessionCommandHandler(eventRepository, signInManager, userManager, userRepository).HandleAsync(new CreateSessionCommand("user@example.com", "Test-Password123!", false, "Browser", "127.0.0.1"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
        await signInManager.DidNotReceive().PasswordSignInAsync(Arg.Any<IdentityUser>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>());
        await eventRepository.DidNotReceive().AddAsync(Arg.Any<IEvent>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldThrowInvalidOperationExceptionWhenSignInThrows()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        CreateSessionCommandHandler commandHandler = new(eventRepository, signInManager, userManager, MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name")));
        InvalidOperationException expectedExceptionInvalidOperationException = new("Identity unavailable.");
        signInManager.PasswordSignInAsync(identityUser, Arg.Any<string>(), false, true).Returns(Task.FromException<SignInResult>(expectedExceptionInvalidOperationException));

        InvalidOperationException invalidOperationException = await Assert.ThrowsAsync<InvalidOperationException>(() => commandHandler.HandleAsync(new CreateSessionCommand("user@example.com", "Test-Password123!", false, "Browser", "127.0.0.1"), TestContext.Current.CancellationToken));

        Assert.Same(expectedExceptionInvalidOperationException, invalidOperationException);
        Assert.False(signInManager.Context.Items.ContainsKey(IdentityConstants.SessionTrackedDetailsProperty));
        await eventRepository.DidNotReceive().AddAsync(Arg.Any<IEvent>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenTheCredentialsAreInvalid()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        CreateSessionCommandHandler commandHandler = new(eventRepository, signInManager, userManager, MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name")));
        signInManager.PasswordSignInAsync(identityUser, Arg.Any<string>(), false, true).Returns(Task.FromResult(SignInResult.Failed));

        Result result = await commandHandler.HandleAsync(new CreateSessionCommand("user@example.com", "Test-Password123!", false, "Browser", "127.0.0.1"), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
        await eventRepository.DidNotReceive().AddAsync(Arg.Any<IEvent>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldThrowInvalidOperationExceptionWhenTheSignInEventCannotBePersisted()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        CreateSessionCommandHandler commandHandler = new(eventRepository, signInManager, userManager, MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name")));
        InvalidOperationException expectedExceptionInvalidOperationException = new("Outbox unavailable.");
        eventRepository.AddAsync(Arg.Any<IEvent>(), true, Arg.Any<CancellationToken>()).Returns(Task.FromException(expectedExceptionInvalidOperationException));

        InvalidOperationException invalidOperationException = await Assert.ThrowsAsync<InvalidOperationException>(() => commandHandler.HandleAsync(new CreateSessionCommand("user@example.com", "Test-Password123!", false, "Browser", "127.0.0.1"), TestContext.Current.CancellationToken));

        Assert.Same(expectedExceptionInvalidOperationException, invalidOperationException);
        await signInManager.Received(1).SignOutAsync();
        Assert.False(signInManager.Context.Items.ContainsKey(IdentityConstants.SessionTrackedDetailsProperty));
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

    private static IUserRepository MockUserRepository(User user)
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        return userRepository;
    }
}
