using System.Security.Claims;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions.EndSession;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Sessions.EndSession;

public class EndSessionCommandHandlerTests
{
    [Fact]
    public async Task ShouldReturnSuccessAndSignOutTheCurrentSession()
    {
        using UserManager<IdentityUser> userManager = MockUserManager();
        SignInManager<IdentityUser> signInManager = MockSignInManager(userManager);

        Result result = await new EndSessionCommandHandler(signInManager).HandleAsync(new EndSessionCommand(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await signInManager.Received(1).SignOutAsync();
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
}
