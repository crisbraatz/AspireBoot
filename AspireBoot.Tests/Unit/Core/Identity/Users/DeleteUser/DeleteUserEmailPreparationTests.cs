using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Identity.Users.DeleteUser;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Tests.Unit.Core.Identity.Users.DeleteUser;

public class DeleteUserEmailPreparationTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public void CanSendShouldReturnTheAccountEligibility(bool isActive, bool isEmailConfirmed)
    {
        User user = new("account", "user@example.com", "User Name");
        if (!isActive)
            user.Deactivate();

        bool allowed = ((IEmailPreparation)new DeleteUserEmailPreparation()).CanSend(new IdentityUser { EmailConfirmed = isEmailConfirmed }, user);

        Assert.Equal(!isActive, allowed);
    }

    [Fact]
    public async Task PrepareAsyncShouldReturnThePreparedEmail()
    {
        IEmailPreparation emailPreparation = new DeleteUserEmailPreparation();
        DeleteUserEvent deleteUserEvent = new("account", "user@example.com", DateTimeOffset.UtcNow);

        Result<EmailDetails> result = await emailPreparation.PrepareAsync(new IdentityUser { Id = "account", Email = "user@example.com", EmailConfirmed = true }, new User("account", "user@example.com", "User Name"), deleteUserEvent, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(deleteUserEvent.EmailRequestKind, emailPreparation.EmailRequestKind);
        Assert.Equal(typeof(DeleteUserEvent), emailPreparation.EventType);
        Assert.False(emailPreparation.ApplyRequestLimit);
        Assert.Equal("user@example.com", result.Value!.Recipient);
        Assert.Equal("Your AspireBoot account was deleted", result.Value.Subject);
    }
}
