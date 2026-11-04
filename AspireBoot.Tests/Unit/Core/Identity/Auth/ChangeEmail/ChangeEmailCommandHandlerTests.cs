using System.Security.Claims;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ChangeEmail;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Messaging;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Auth.ChangeEmail;

public class ChangeEmailCommandHandlerTests
{
    [Fact]
    public async Task ShouldReturnSuccessAndPublishARequestForTheNormalizedNewEmail()
    {
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);

        Result result = await new ChangeEmailCommandHandler(MockEmailRequestLimiter(), eventRepository, MockSignInManager(userManager), userManager, MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name"))).HandleAsync(new ChangeEmailCommand("NEW@EXAMPLE.COM", "Test-Password123!", new ClaimsPrincipal(), "https://app.tests"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await eventRepository.Received(1).AddAsync(Arg.Is<ChangeEmailEvent>(changeEmailEvent => changeEmailEvent.NewEmail == "new@example.com"), true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenTheIdentityUserIsUnknown()
    {
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        ChangeEmailCommandHandler commandHandler = new(MockEmailRequestLimiter(), eventRepository, MockSignInManager(userManager), userManager, MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name")));
        userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(Task.FromResult<IdentityUser?>(null));

        Result result = await commandHandler.HandleAsync(new ChangeEmailCommand("NEW@EXAMPLE.COM", "Test-Password123!", new ClaimsPrincipal(), "https://app.tests"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
        await eventRepository.DidNotReceive().AddAsync(Arg.Any<IEvent>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenTheUserIsMissing()
    {
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        IUserRepository userRepository = MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name"));
        ChangeEmailCommandHandler commandHandler = new(MockEmailRequestLimiter(), eventRepository, MockSignInManager(userManager), userManager, userRepository);
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));

        Result result = await commandHandler.HandleAsync(new ChangeEmailCommand("NEW@EXAMPLE.COM", "Test-Password123!", new ClaimsPrincipal(), "https://app.tests"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
        await eventRepository.DidNotReceive().AddAsync(Arg.Any<IEvent>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenTheCredentialsAreInvalid()
    {
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);
        ChangeEmailCommandHandler commandHandler = new(MockEmailRequestLimiter(), eventRepository, signInManager, userManager, MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name")));
        signInManager.CheckPasswordSignInAsync(identityUser, Arg.Any<string>(), true).Returns(Task.FromResult(SignInResult.Failed));

        Result result = await commandHandler.HandleAsync(new ChangeEmailCommand("NEW@EXAMPLE.COM", "Test-Password123!", new ClaimsPrincipal(), "https://app.tests"), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
        await eventRepository.DidNotReceive().AddAsync(Arg.Any<IEvent>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnFailureWhenPreservingEmailLimiterFailures()
    {
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        ChangeEmailCommandHandler commandHandler = new(emailRequestLimiter, eventRepository, MockSignInManager(userManager), userManager, MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name")));
        emailRequestLimiter.CanSendAsync("account", "new@example.com", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<bool>.Success(false)));
        Failure failure = Failure.ServiceUnavailable("email.limiter-unavailable", "Unavailable.");
        emailRequestLimiter.CanSendAsync("account", "new@example.com", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<bool>.Failure([failure])));

        Result result = await commandHandler.HandleAsync(new ChangeEmailCommand("NEW@EXAMPLE.COM", "Test-Password123!", new ClaimsPrincipal(), "https://app.tests"), TestContext.Current.CancellationToken);

        Assert.Equal(failure, Assert.Single(result.Failures!));
        await eventRepository.DidNotReceive().AddAsync(Arg.Any<IEvent>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnTooManyRequestsFailureWhenAnExhaustedEmailQuotaIsProvided()
    {
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter();
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        ChangeEmailCommandHandler commandHandler = new(emailRequestLimiter, eventRepository, MockSignInManager(userManager), userManager, MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name")));
        emailRequestLimiter.CanSendAsync("account", "new@example.com", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<bool>.Success(false)));

        Result result = await commandHandler.HandleAsync(new ChangeEmailCommand("NEW@EXAMPLE.COM", "Test-Password123!", new ClaimsPrincipal(), "https://app.tests"), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.TooManyRequests, Assert.Single(result.Failures!).FailureKind);
        await eventRepository.DidNotReceive().AddAsync(Arg.Any<IEvent>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    private static IEmailRequestLimiter MockEmailRequestLimiter()
    {
        IEmailRequestLimiter emailRequestLimiter = Substitute.For<IEmailRequestLimiter>();
        emailRequestLimiter.CanSendAsync("account", "new@example.com", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<bool>.Success(true)));
        return emailRequestLimiter;
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
