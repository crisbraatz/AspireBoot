using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ConfirmEmail;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Tests.Unit.Core.Identity.Auth.ConfirmEmail;

public class ConfirmEmailPreparationTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void CanSendShouldReturnTheAccountEligibility(bool isActive, bool isEmailConfirmed)
    {
        User user = new("account", "user@example.com", "User Name");
        if (!isActive)
            user.Deactivate();

        bool allowed = ((IEmailPreparation)new ConfirmEmailPreparation()).CanSend(new IdentityUser { EmailConfirmed = isEmailConfirmed }, user);

        Assert.Equal(isActive && isEmailConfirmed, allowed);
    }

    [Fact]
    public async Task PrepareAsyncShouldReturnThePreparedEmail()
    {
        IEmailPreparation emailPreparation = new ConfirmEmailPreparation();
        ConfirmEmailEvent confirmEmailEvent = new("account", "user@example.com");

        Result<EmailDetails> result = await emailPreparation.PrepareAsync(new IdentityUser { Id = "account", Email = "user@example.com", EmailConfirmed = true }, new User("account", "user@example.com", "User Name"), confirmEmailEvent, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(confirmEmailEvent.EmailRequestKind, emailPreparation.EmailRequestKind);
        Assert.Equal(typeof(ConfirmEmailEvent), emailPreparation.EventType);
        Assert.False(emailPreparation.ApplyRequestLimit);
        Assert.Equal("user@example.com", result.Value!.Recipient);
        Assert.Equal("Welcome to AspireBoot", result.Value.Subject);
    }
}
