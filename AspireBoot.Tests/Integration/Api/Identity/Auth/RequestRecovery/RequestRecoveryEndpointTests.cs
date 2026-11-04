using System.Net;
using System.Net.Http.Json;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AspireBoot.Tests.Integration.Api.Identity.Auth.RequestRecovery;

[Trait("Category", "Integration")]
public class RequestRecoveryEndpointTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldReturnNoContentWhenQueueingTheRequestedEmail()
    {
        await CreateAccountAsync(isEmailConfirmed: true);

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/forgot-password", new { email = "user@example.com" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        EmailDetails emailDetails = await WaitForEmailAsync("Reset your AspireBoot password");
        Assert.Equal("user@example.com", emailDetails.Recipient);
        Assert.Contains("token=", emailDetails.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnNoContentWhenTheAccountIsIneligible()
    {
        await CreateAccountAsync(isEmailConfirmed: false);

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/forgot-password", new { email = "user@example.com" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnBadRequestWhenTheAddressIsInvalid()
    {
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/forgot-password", new { email = "invalid" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnNoContentWhenTheAccountIsUnknown()
    {
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/forgot-password", new { email = "unknown@example.com" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().ReceivedCalls());
    }

    [Fact]
    public async Task ShouldReturnNoContentWhenTheProfileIsMissing()
    {
        await CreateAccountAsync(isEmailConfirmed: true);
        await RemoveProfileAsync();

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/forgot-password", new { email = "user@example.com" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().DidNotReceive().SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>());
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

    private async Task<EmailDetails> WaitForEmailAsync(string subject)
    {
        using CancellationTokenSource timeoutCancellationTokenSource = new(TimeSpan.FromSeconds(20));
        while (true)
        {
            EmailDetails? emailDetails = TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IEmailSender>().ReceivedCalls().Where(call => call.GetMethodInfo().Name is nameof(IEmailSender.SendAsync)).Select(call => (EmailDetails)call.GetArguments()[0]!).FirstOrDefault(messageEmailDetails => messageEmailDetails.Subject == subject);
            if (emailDetails is not null)
                return emailDetails;
            await Task.Delay(50, timeoutCancellationTokenSource.Token);
        }
    }
}
