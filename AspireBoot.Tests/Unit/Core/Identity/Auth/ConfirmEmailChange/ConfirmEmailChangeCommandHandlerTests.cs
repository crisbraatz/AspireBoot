using System.Security.Claims;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ConfirmEmailChange;
using AspireBoot.Core.Identity.Sessions;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Caching;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Auth.ConfirmEmailChange;

public class ConfirmEmailChangeCommandHandlerTests
{
    [Fact]
    public async Task ShouldReturnSuccessWhenChangingEmailInvalidateCacheWhenRevokingSessions()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        User user = new(identityUser.Id, identityUser.Email, "User Name");
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        IUserRepository userRepository = MockUserRepository(user);
        ICache cache = Substitute.For<ICache>();
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();
        userRepository.TryUpdateEmailAsync(user, identityUser, "new@example.com", "token", Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Success));

        Result result = await new ConfirmEmailChangeCommandHandler(cache, sessionsTicketStore, userManager, userRepository).HandleAsync(new ConfirmEmailChangeCommand("NEW@EXAMPLE.COM", "account", "token"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await cache.Received(1).RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await sessionsTicketStore.Received(1).RemoveOthersAsync("account", string.Empty, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedFailureWhenTheIdentityUserIsUnknown()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        User user = new(identityUser.Id, identityUser.Email, "User Name");
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        IUserRepository userRepository = MockUserRepository(user);
        userRepository.TryUpdateEmailAsync(user, identityUser, "new@example.com", "token", Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Success));
        ConfirmEmailChangeCommandHandler commandHandler = new(Substitute.For<ICache>(), Substitute.For<ISessionsTicketStore>(), userManager, userRepository);
        userManager.FindByIdAsync("account").Returns(Task.FromResult<IdentityUser?>(null));

        Result result = await commandHandler.HandleAsync(new ConfirmEmailChangeCommand("NEW@EXAMPLE.COM", "account", "token"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(FailureKind.Unauthorized, Assert.Single(result.Failures!).FailureKind);
    }

    [Fact]
    public async Task ShouldReturnNotFoundFailureWhenTheUserIsMissing()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        User user = new(identityUser.Id, identityUser.Email, "User Name");
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        IUserRepository userRepository = MockUserRepository(user);
        userRepository.TryUpdateEmailAsync(user, identityUser, "new@example.com", "token", Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Success));
        ConfirmEmailChangeCommandHandler commandHandler = new(Substitute.For<ICache>(), Substitute.For<ISessionsTicketStore>(), userManager, userRepository);
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(null));

        Result result = await commandHandler.HandleAsync(new ConfirmEmailChangeCommand("NEW@EXAMPLE.COM", "account", "token"), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.NotFound, Assert.Single(result.Failures!).FailureKind);
    }

    [Theory]
    [InlineData("DuplicateEmail", "email")]
    [InlineData("InvalidEmail", "email")]
    [InlineData("InvalidToken", "token")]
    [InlineData("ConcurrencyFailure", "")]
    public async Task ShouldReturnFailureWhenMappingIdentityFailures(string code, string field)
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        User user = new(identityUser.Id, identityUser.Email, "User Name");
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        IUserRepository userRepository = MockUserRepository(user);
        ICache cache = Substitute.For<ICache>();
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();
        userRepository.TryUpdateEmailAsync(user, identityUser, "new@example.com", "token", Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Success));
        ConfirmEmailChangeCommandHandler commandHandler = new(cache, sessionsTicketStore, userManager, userRepository);
        userRepository.TryUpdateEmailAsync(user, identityUser, "new@example.com", "token", Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = code })));

        Result result = await commandHandler.HandleAsync(new ConfirmEmailChangeCommand("NEW@EXAMPLE.COM", "account", "token"), TestContext.Current.CancellationToken);

        Failure failure = Assert.Single(result.Failures!);
        Assert.Equal("identity.auth." + code, failure.Code);
        Assert.Equal(field, failure.Field);
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await sessionsTicketStore.DidNotReceive().RemoveOthersAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
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
