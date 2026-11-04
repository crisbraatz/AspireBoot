using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions;
using AspireBoot.Core.Identity.Sessions.CreateSession;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Tests.Unit.Core.Identity.Sessions.CreateSession;

public class CreateSessionEmailPreparationTests
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

        bool allowed = ((IEmailPreparation)new CreateSessionEmailPreparation()).CanSend(new IdentityUser { EmailConfirmed = isEmailConfirmed }, user);

        Assert.Equal(isActive, allowed);
    }

    [Fact]
    public async Task PrepareAsyncShouldReturnThePreparedEmail()
    {
        IEmailPreparation emailPreparation = new CreateSessionEmailPreparation();
        CreateSessionEvent createSessionEvent = new("account", "user@example.com", DateTimeOffset.UtcNow, new TrackedDetails("Chrome", "Linux", "Desktop", "127.0.0.1"));

        Result<EmailDetails> result = await emailPreparation.PrepareAsync(new IdentityUser { Id = "account", Email = "user@example.com", EmailConfirmed = true }, new User("account", "user@example.com", "User Name"), createSessionEvent, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(createSessionEvent.EmailRequestKind, emailPreparation.EmailRequestKind);
        Assert.Equal(typeof(CreateSessionEvent), emailPreparation.EventType);
        Assert.False(emailPreparation.ApplyRequestLimit);
        Assert.Equal("user@example.com", result.Value!.Recipient);
        Assert.Equal("New sign-in to your AspireBoot account", result.Value.Subject);
    }

    [Fact]
    public async Task PrepareAsyncShouldReturnSuccessWhenDescribeAnUnavailableIpAddress()
    {
        Result<EmailDetails> result = await ((IEmailPreparation)new CreateSessionEmailPreparation()).PrepareAsync(new IdentityUser { Id = "account", Email = "user@example.com", EmailConfirmed = true }, new User("account", "user@example.com", "User Name"), new CreateSessionEvent("account", "user@example.com", DateTimeOffset.UtcNow, new TrackedDetails("Chrome", "Linux", "Desktop")), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains("IP: Unavailable", result.Value!.Text, StringComparison.Ordinal);
        Assert.Contains("Unavailable", result.Value.Html, StringComparison.Ordinal);
    }
}
