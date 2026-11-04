using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ConfirmEmail;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Wolverine.Tracking;

namespace AspireBoot.Tests.Integration.Worker.Identity.Auth.ConfirmEmail;

[Trait("Category", "Integration")]
public class ConfirmEmailEventConsumerTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldDeliverTheExpectedEmailThroughTheWorker()
    {
        await CreateAccountAsync(isEmailConfirmed: true);

        await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().PublishMessageAndWaitAsync(new ConfirmEmailEvent("account", "user@example.com"));

        EmailDetails emailDetails = Assert.Single(TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().ReceivedCalls().Where(call => call.GetMethodInfo().Name is nameof(IEmailSender.SendAsync)).Select(call => (EmailDetails)call.GetArguments()[0]!));
        Assert.Equal("Welcome to AspireBoot", emailDetails.Subject);
        Assert.Equal("user@example.com", emailDetails.Recipient);
    }

    [Fact]
    public async Task ShouldQuarantineInvalidEventData()
    {
        await CreateAccountAsync(isEmailConfirmed: true);
        ConfirmEmailEvent confirmEmailEvent = new("account", "user@example.com");
        confirmEmailEvent = confirmEmailEvent with { Email = "invalid" };

        ITrackedSession trackedSession = await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().IgnoreMessageType<Wolverine.Runtime.RemoteInvocation.Acknowledgement>().DoNotAssertOnExceptionsDetected().Timeout(TimeSpan.FromSeconds(15)).PublishMessageAndWaitAsync(confirmEmailEvent);

        Assert.Contains(trackedSession.AllExceptions(), exception => exception is InvalidEmailRequestException);
        Assert.Single(trackedSession.MovedToErrorQueue.AllMessages());
        await TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldSkipAnUnknownAccount()
    {
        await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().PublishMessageAndWaitAsync(new ConfirmEmailEvent("account", "user@example.com"));

        await TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldSkipAnOutdatedEmailAddress()
    {
        await CreateAccountAsync(isEmailConfirmed: true);
        await ChangeAccountEmailAsync();

        await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().PublishMessageAndWaitAsync(new ConfirmEmailEvent("account", "user@example.com"));

        await TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldSkipAMissingProfile()
    {
        await CreateAccountAsync(isEmailConfirmed: true);
        await RemoveProfileAsync();

        await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().PublishMessageAndWaitAsync(new ConfirmEmailEvent("account", "user@example.com"));

        await TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldSkipAnIneligibleProfile()
    {
        await CreateAccountAsync(isEmailConfirmed: true);
        await SetProfileActiveAsync(false);

        await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().PublishMessageAndWaitAsync(new ConfirmEmailEvent("account", "user@example.com"));

        await TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldSkipAnIneligibleConfirmationState()
    {
        await CreateAccountAsync(isEmailConfirmed: true);
        await SetEmailConfirmedAsync(false);

        await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().PublishMessageAndWaitAsync(new ConfirmEmailEvent("account", "user@example.com"));

        await TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldRetryATemporaryEmailDeliveryFailure()
    {
        await CreateAccountAsync(isEmailConfirmed: true);
        IEmailSender emailSender = TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>();
        ConfirmEmailEvent confirmEmailEvent = new("account", "user@example.com");
        emailSender.SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.ServiceUnavailable("smtp.unavailable", "Unavailable.")])), Task.FromResult(Result.Success()));

        ITrackedSession trackedSession = await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().IgnoreMessageType<Wolverine.Runtime.RemoteInvocation.Acknowledgement>().DoNotAssertOnExceptionsDetected().Timeout(TimeSpan.FromSeconds(15)).PublishMessageAndWaitAsync(confirmEmailEvent);

        Assert.Equal(confirmEmailEvent.MessageId, trackedSession.MessageSucceeded.SingleMessage<ConfirmEmailEvent>().MessageId);
        Assert.Empty(trackedSession.MovedToErrorQueue.AllMessages());
        await emailSender.Received(2).SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    private async Task ChangeAccountEmailAsync()
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        UserManager<IdentityUser> userManager = asyncServiceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.True((await userManager.SetEmailAsync((await userManager.FindByIdAsync("account"))!, "changed@example.com")).Succeeded);
    }

    private async Task CreateAccountAsync(string identityUserId = "account", string email = "user@example.com", bool isEmailConfirmed = true, string role = "User")
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        UserManager<IdentityUser> userManager = asyncServiceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        IdentityUser? identityUser = await userManager.FindByIdAsync(identityUserId);
        if (identityUser is null)
        {
            identityUser = new IdentityUser { Id = identityUserId, UserName = identityUserId, Email = email, EmailConfirmed = isEmailConfirmed };
            Assert.True((await userManager.CreateAsync(identityUser, "Test-Password123!")).Succeeded);
        }
        RoleManager<IdentityRole> roleManager = asyncServiceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(role))
            Assert.True((await roleManager.CreateAsync(new IdentityRole(role))).Succeeded);
        if (!await userManager.IsInRoleAsync(identityUser, role))
            Assert.True((await userManager.AddToRoleAsync(identityUser, role)).Succeeded);
        AspireBootDbContext aspireBootDbContext = asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>();
        User? profileUser = await aspireBootDbContext.Users.SingleOrDefaultAsync(user => user.IdentityUserId == identityUserId);
        if (profileUser is null)
        {
            profileUser = new User(identityUserId, email, "User Name");
            aspireBootDbContext.Users.Add(profileUser);
            await aspireBootDbContext.SaveChangesAsync();
        }
    }

    private async Task RemoveProfileAsync(string identityUserId = "account")
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        await asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().Users.Where(user => user.IdentityUserId == identityUserId).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    private async Task SetEmailConfirmedAsync(bool isEmailConfirmed)
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        UserManager<IdentityUser> userManager = asyncServiceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        IdentityUser identityUser = (await userManager.FindByIdAsync("account"))!;
        identityUser.EmailConfirmed = isEmailConfirmed;
        Assert.True((await userManager.UpdateAsync(identityUser)).Succeeded);
    }

    private async Task SetProfileActiveAsync(bool isActive)
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        await asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().Users.Where(user => user.IdentityUserId == "account").ExecuteUpdateAsync(settersUpdateSettersBuilder => settersUpdateSettersBuilder.SetProperty(user => user.IsActive, isActive), TestContext.Current.CancellationToken);
    }
}
