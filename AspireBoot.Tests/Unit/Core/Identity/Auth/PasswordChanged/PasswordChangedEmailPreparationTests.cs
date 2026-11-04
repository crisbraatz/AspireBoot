using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.PasswordChanged;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Tests.Unit.Core.Identity.Auth.PasswordChanged;

public class PasswordChangedEmailPreparationTests
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

        bool allowed = ((IEmailPreparation)new PasswordChangedEmailPreparation()).CanSend(new IdentityUser { EmailConfirmed = isEmailConfirmed }, user);

        Assert.Equal(isActive, allowed);
    }

    [Fact]
    public async Task PrepareAsyncShouldReturnThePreparedEmail()
    {
        IEmailPreparation emailPreparation = new PasswordChangedEmailPreparation();
        PasswordChangedEvent passwordChangedEvent = new("account", "user@example.com");

        Result<EmailDetails> result = await emailPreparation.PrepareAsync(new IdentityUser { Id = "account", Email = "user@example.com", EmailConfirmed = true }, new User("account", "user@example.com", "User Name"), passwordChangedEvent, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(passwordChangedEvent.EmailRequestKind, emailPreparation.EmailRequestKind);
        Assert.Equal(typeof(PasswordChangedEvent), emailPreparation.EventType);
        Assert.False(emailPreparation.ApplyRequestLimit);
        Assert.Equal("user@example.com", result.Value!.Recipient);
        Assert.Equal("Your AspireBoot password was changed", result.Value.Subject);
    }
}
