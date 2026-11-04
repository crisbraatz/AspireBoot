using System.Security.Claims;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ResendEmailConfirmation;
using AspireBoot.Core.Identity.Auth.SendEmailConfirmation;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Messaging;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Auth.ResendEmailConfirmation;

public class ResendEmailConfirmationCommandHandlerTests
{
    [Fact]
    public async Task ShouldReturnSuccessWhenQueueingEmailForAnEligibleAccount()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = false };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        IEventRepository eventRepository = Substitute.For<IEventRepository>();

        Result result = await new ResendEmailConfirmationCommandHandler(eventRepository, userManager, MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name"))).HandleAsync(new ResendEmailConfirmationCommand("USER@EXAMPLE.COM", "https://app.tests"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await eventRepository.Received(1).AddAsync(Arg.Is<SendEmailConfirmationEvent>(sendEmailConfirmationEvent => sendEmailConfirmationEvent.Email == "user@example.com"), true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenSkippingAnIneligibleAccount()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = false };
        User user = new(identityUser.Id, identityUser.Email, "User Name");
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        identityUser.EmailConfirmed = true;

        Result result = await new ResendEmailConfirmationCommandHandler(eventRepository, userManager, MockUserRepository(user)).HandleAsync(new ResendEmailConfirmationCommand("USER@EXAMPLE.COM", "https://app.tests"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await eventRepository.DidNotReceive().AddAsync(Arg.Any<IEvent>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnTheSameSuccessForAnUnknownAccount()
    {
        using UserManager<IdentityUser> userManager = MockUserManager();
        IEventRepository eventRepository = Substitute.For<IEventRepository>();

        Result result = await new ResendEmailConfirmationCommandHandler(eventRepository, userManager, Substitute.For<IUserRepository>()).HandleAsync(new ResendEmailConfirmationCommand("USER@EXAMPLE.COM", "https://app.tests"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await eventRepository.DidNotReceive().AddAsync(Arg.Any<IEvent>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenSkipAMissingUser()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = false };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        IUserRepository userRepository = MockUserRepository(new User(identityUser.Id, identityUser.Email, "User Name"));
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        ResendEmailConfirmationCommandHandler commandHandler = new(eventRepository, userManager, userRepository);
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));

        Result result = await commandHandler.HandleAsync(new ResendEmailConfirmationCommand("USER@EXAMPLE.COM", "https://app.tests"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await eventRepository.DidNotReceive().AddAsync(Arg.Any<IEvent>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
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
