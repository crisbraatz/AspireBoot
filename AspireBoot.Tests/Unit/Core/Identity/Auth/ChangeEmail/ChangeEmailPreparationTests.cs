using System.Security.Claims;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ChangeEmail;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Auth.ChangeEmail;

public class ChangeEmailPreparationTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void CanSendShouldReturnTheAccountEligibility(bool isActive, bool isEmailConfirmed)
    {
        IdentityUser identityUser = new() { EmailConfirmed = isEmailConfirmed };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        User user = new("account", "user@example.com", "User Name");
        if (!isActive)
            user.Deactivate();

        bool allowed = ((IEmailPreparation)new ChangeEmailPreparation(userManager)).CanSend(identityUser, user);

        Assert.Equal(isActive, allowed);
    }

    [Fact]
    public async Task PrepareAsyncShouldReturnThePreparedEmail()
    {
        IdentityUser identityUser = new() { Id = "account", Email = "user@example.com", EmailConfirmed = true };
        using UserManager<IdentityUser> userManager = MockUserManager(identityUser);
        IEmailPreparation emailPreparation = new ChangeEmailPreparation(userManager);
        ChangeEmailEvent changeEmailEvent = new("account", "user@example.com", "new@example.com", "https://app.tests");
        userManager.GenerateChangeEmailTokenAsync(identityUser, "new@example.com").Returns(Task.FromResult("token +/&?"));

        Result<EmailDetails> result = await emailPreparation.PrepareAsync(identityUser, new User("account", "user@example.com", "User Name"), changeEmailEvent, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(changeEmailEvent.EmailRequestKind, emailPreparation.EmailRequestKind);
        Assert.Equal(typeof(ChangeEmailEvent), emailPreparation.EventType);
        Assert.True(emailPreparation.ApplyRequestLimit);
        Assert.Equal("new@example.com", result.Value!.Recipient);
        Assert.Equal("Confirm your new AspireBoot email", result.Value.Subject);
        string link = System.Text.RegularExpressions.Regex.Match(result.Value.Text, @"https://[^\s]+", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(1)).Value;
        Assert.Equal("/auth/confirm-email-change", new Uri(link).AbsolutePath);
        Assert.Equal("token +/&?", Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(link).Query)["token"].ToString());
    }

    [Fact]
    public void ValidateShouldReturnSuccessWhenAcceptingAValidRecipient()
    {
        using UserManager<IdentityUser> userManager = MockUserManager();

        Result result = ((IEmailPreparation)new ChangeEmailPreparation(userManager)).Validate(new ChangeEmailEvent("account", "user@example.com", "new@example.com", "https://app.tests"));

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("oversized")]
    [InlineData("invalid")]
    public void ValidateShouldReturnFailureWhenEachInvalidRecipientConditionAreProvided(string recipient)
    {
        using UserManager<IdentityUser> userManager = MockUserManager();

        Result result = ((IEmailPreparation)new ChangeEmailPreparation(userManager)).Validate(new ChangeEmailEvent("account", "user@example.com", recipient is "oversized" ? new string('a', 255) + "@example.com" : recipient, "https://app.tests"));

        Failure failure = Assert.Single(result.Failures!);
        Assert.Equal("identity.auth.invalid-email", failure.Code);
        Assert.Equal("newEmail", failure.Field);
    }

    private static UserManager<IdentityUser> MockUserManager(IdentityUser? identityUser = null)
    {
        UserManager<IdentityUser> userManager = Substitute.For<UserManager<IdentityUser>>(Substitute.For<IUserStore<IdentityUser>>(), Options.Create(new IdentityOptions()), Substitute.For<IPasswordHasher<IdentityUser>>(), Array.Empty<IUserValidator<IdentityUser>>(), Array.Empty<IPasswordValidator<IdentityUser>>(), Substitute.For<ILookupNormalizer>(), new IdentityErrorDescriber(), Substitute.For<IServiceProvider>(), NullLogger<UserManager<IdentityUser>>.Instance);
        userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(Task.FromResult(identityUser));
        userManager.FindByEmailAsync(Arg.Any<string>()).Returns(Task.FromResult(identityUser));
        userManager.FindByIdAsync(Arg.Any<string>()).Returns(Task.FromResult(identityUser));
        return userManager;
    }
}
