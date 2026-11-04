using System.Security.Cryptography;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.SendEmailConfirmation;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using StackExchange.Redis;
using Wolverine.Tracking;

namespace AspireBoot.Tests.Integration.Worker.Identity.Auth.SendEmailConfirmation;

[Trait("Category", "Integration")]
public class SendEmailConfirmationEventConsumerTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldDeliverTheExpectedEmailThroughTheWorker()
    {
        await CreateAccountAsync(isEmailConfirmed: false);

        await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().PublishMessageAndWaitAsync(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests"));

        EmailDetails emailDetails = Assert.Single(TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().ReceivedCalls().Where(call => call.GetMethodInfo().Name is nameof(IEmailSender.SendAsync)).Select(call => (EmailDetails)call.GetArguments()[0]!));
        Assert.Equal("Confirm your AspireBoot email", emailDetails.Subject);
        Assert.Equal("user@example.com", emailDetails.Recipient);
    }

    [Fact]
    public async Task ShouldQuarantineInvalidEventData()
    {
        await CreateAccountAsync(isEmailConfirmed: false);
        SendEmailConfirmationEvent sendEmailConfirmationEvent = new("account", "user@example.com", "https://app.tests");
        sendEmailConfirmationEvent = sendEmailConfirmationEvent with { Email = "invalid" };

        ITrackedSession trackedSession = await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().IgnoreMessageType<Wolverine.Runtime.RemoteInvocation.Acknowledgement>().DoNotAssertOnExceptionsDetected().Timeout(TimeSpan.FromSeconds(15)).PublishMessageAndWaitAsync(sendEmailConfirmationEvent);

        Assert.Contains(trackedSession.AllExceptions(), exception => exception is InvalidEmailRequestException);
        Assert.Single(trackedSession.MovedToErrorQueue.AllMessages());
        await TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldSkipAnUnknownAccount()
    {
        await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().PublishMessageAndWaitAsync(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests"));

        await TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldSkipAnOutdatedEmailAddress()
    {
        await CreateAccountAsync(isEmailConfirmed: false);
        await ChangeAccountEmailAsync();

        await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().PublishMessageAndWaitAsync(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests"));

        await TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldSkipAMissingProfile()
    {
        await CreateAccountAsync(isEmailConfirmed: false);
        await RemoveProfileAsync();

        await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().PublishMessageAndWaitAsync(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests"));

        await TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldSkipAnIneligibleProfile()
    {
        await CreateAccountAsync(isEmailConfirmed: false);
        await SetProfileActiveAsync(false);

        await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().PublishMessageAndWaitAsync(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests"));

        await TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldSkipAnIneligibleConfirmationState()
    {
        await CreateAccountAsync(isEmailConfirmed: false);
        await SetEmailConfirmedAsync(true);

        await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().PublishMessageAndWaitAsync(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests"));

        await TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnTooManyRequestsFailureWhenTheQuotaIsFull()
    {
        await CreateAccountAsync(isEmailConfirmed: false);
        await FillAccountQuotaAsync();
        await using AsyncServiceScope asyncServiceScope = TestFixture.Host.Services.CreateAsyncScope();

        Result result = await asyncServiceScope.ServiceProvider.GetRequiredService<ICommandHandler<SendRequestedEmailCommand>>().HandleAsync(new SendRequestedEmailCommand(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests")), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.TooManyRequests, Assert.Single(result.Failures!).FailureKind);
        await TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldRetryATemporaryEmailDeliveryFailure()
    {
        await CreateAccountAsync(isEmailConfirmed: false);
        IEmailSender emailSender = TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>();
        SendEmailConfirmationEvent sendEmailConfirmationEvent = new("account", "user@example.com", "https://app.tests");
        emailSender.SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.ServiceUnavailable("smtp.unavailable", "Unavailable.")])), Task.FromResult(Result.Success()));

        ITrackedSession trackedSession = await TestFixture.Host.Services.TrackActivity().IncludeExternalTransports().IgnoreMessageType<Wolverine.Runtime.RemoteInvocation.Acknowledgement>().DoNotAssertOnExceptionsDetected().Timeout(TimeSpan.FromSeconds(15)).PublishMessageAndWaitAsync(sendEmailConfirmationEvent);

        Assert.Equal(sendEmailConfirmationEvent.MessageId, trackedSession.MessageSucceeded.SingleMessage<SendEmailConfirmationEvent>().MessageId);
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

    private async Task FillAccountQuotaAsync() => await TestFixture.Host.Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase().SortedSetAddAsync("identity:email:{requests}:account:" + Convert.ToHexString(SHA256.HashData("account"u8.ToArray())), [.. Enumerable.Range(0, 10).Select(index => new SortedSetEntry("reserved-" + index, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))]);

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
