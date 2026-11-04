using System.Security.Claims;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.RequestRecovery;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Auth.RequestRecovery;

public class RequestRecoveryEmailPreparationTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void CanSendShouldReturnTheAccountEligibility(bool isActive, bool isEmailConfirmed)
    {
        IdentityUser identityUser = new() { EmailConfirmed = isEmailConfirmed };
        User user = new("account", "user@example.com", "User Name");
        if (!isActive)
            user.Deactivate();
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);

        bool allowed = ((IEmailPreparation)new RequestRecoveryEmailPreparation(userManager)).CanSend(identityUser, user);

        Assert.Equal(isActive && isEmailConfirmed, allowed);
    }

    [Fact]
    public async Task PrepareAsyncShouldReturnThePreparedEmail()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        userManager.GeneratePasswordResetTokenAsync(identityUser).Returns(Task.FromResult("token +/&?"));
        IEmailPreparation emailPreparation = new RequestRecoveryEmailPreparation(userManager);
        RequestRecoveryEvent requestRecoveryEvent = new("account", "user@example.com", "https://app.tests");

        Result<EmailDetails> result = await emailPreparation.PrepareAsync(identityUser, new User("account", "user@example.com", "User Name"), requestRecoveryEvent, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(requestRecoveryEvent.EmailRequestKind, emailPreparation.EmailRequestKind);
        Assert.Equal(typeof(RequestRecoveryEvent), emailPreparation.EventType);
        Assert.True(emailPreparation.ApplyRequestLimit);
        Assert.Equal("user@example.com", result.Value!.Recipient);
        Assert.Equal("Reset your AspireBoot password", result.Value.Subject);
        string link = System.Text.RegularExpressions.Regex.Match(result.Value.Text, @"https://[^\s]+", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(1)).Value;
        Assert.Equal("/auth/reset-password", new Uri(link).AbsolutePath);
        Assert.Equal("token +/&?", Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(link).Query)["token"].ToString());
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
