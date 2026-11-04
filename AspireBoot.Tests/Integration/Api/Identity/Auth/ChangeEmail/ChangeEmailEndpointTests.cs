using System.Net;
using System.Net.Http.Json;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Smtp;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using StackExchange.Redis;

namespace AspireBoot.Tests.Integration.Api.Identity.Auth.ChangeEmail;

[Trait("Category", "Integration")]
public class ChangeEmailEndpointTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldReturnNoContentWhenRequestingConfirmationAtTheNewAddress()
    {
        await SignInAsync();

        using HttpResponseMessage response = await HttpClient.PatchAsJsonAsync("/api/auth/email", new { newEmail = "new@example.com", password = "Test-Password123!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("new@example.com", (await WaitForEmailAsync("Confirm your new AspireBoot email")).Recipient);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheRequestIsUnauthenticated()
    {
        using HttpClient anonymousHttpClient = TestFixture.ApiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());

        using HttpResponseMessage response = await anonymousHttpClient.PatchAsJsonAsync("/api/auth/email", new { newEmail = "new@example.com", password = "Test-Password123!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("invalid", "Test-Password123!")]
    [InlineData("new@example.com", "short")]
    [InlineData("", "Test-Password123!")]
    public async Task ShouldReturnBadRequestWhenTheRequestIsInvalid(string address, string value)
    {
        await SignInAsync();

        using HttpResponseMessage response = await HttpClient.PatchAsJsonAsync("/api/auth/email", new { newEmail = address, password = value }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheProfileIsMissing()
    {
        await SignInAsync();
        await RemoveProfileAsync();

        using HttpResponseMessage response = await HttpClient.PatchAsJsonAsync("/api/auth/email", new { newEmail = "new@example.com", password = "Test-Password123!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("identity.auth.invalid-credentials", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenThePasswordIsIncorrect()
    {
        await SignInAsync();

        using HttpResponseMessage response = await HttpClient.PatchAsJsonAsync("/api/auth/email", new { newEmail = "new@example.com", password = "Wrong-Password123!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnServiceUnavailableWhenPreservingTheLimiterFailureResponse()
    {
        await using ApiWebApplicationFactory apiWebApplicationFactory = CreateApiFactoryWithFailedLimiter(false);
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());
        await SignInAsync(httpClient);

        using HttpResponseMessage response = await httpClient.PatchAsJsonAsync("/api/auth/email", new { newEmail = "new@example.com", password = "Test-Password123!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("private limiter details", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnServiceUnavailableWhenTheLimiterIsUnavailable()
    {
        await using ApiWebApplicationFactory apiWebApplicationFactory = CreateApiFactoryWithFailedLimiter(true);
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());
        await SignInAsync(httpClient);

        using HttpResponseMessage response = await httpClient.PatchAsJsonAsync("/api/auth/email", new { newEmail = "new@example.com", password = "Test-Password123!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("private limiter details", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnTooManyRequestsWhenTheEmailCooldownIsActive()
    {
        await using ApiWebApplicationFactory apiWebApplicationFactory = TestFixture.CreateApiWebApplicationFactory(new Dictionary<string, string?> { ["AspireBoot:Core:EmailRequests:CooldownInSeconds"] = "60" });
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());
        await SignInAsync(httpClient);
        object request = new { newEmail = "new@example.com", password = "Test-Password123!" };
        using HttpResponseMessage firstResponse = await httpClient.PatchAsJsonAsync("/api/auth/email", request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);

        using HttpResponseMessage response = await httpClient.PatchAsJsonAsync("/api/auth/email", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
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

    private ApiWebApplicationFactory CreateApiFactoryWithFailedLimiter(bool throwException)
    {
        IEmailRequestLimiter emailRequestLimiter = MockEmailRequestLimiter(throwException);
        return TestFixture.CreateApiWebApplicationFactory(new Dictionary<string, string?>(), serviceCollection => serviceCollection.Replace(ServiceDescriptor.Singleton(emailRequestLimiter)));
    }

    private static IEmailRequestLimiter MockEmailRequestLimiter(bool throwException)
    {
        IEmailRequestLimiter emailRequestLimiter = Substitute.For<IEmailRequestLimiter>();
        emailRequestLimiter.CanSendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(throwException ? Task.FromException<Result<bool>>(new RedisConnectionException(ConnectionFailureType.UnableToConnect, CommandFlags.None, "private limiter details")): Task.FromResult(Result<bool>.Failure([Failure.ServiceUnavailable("email.limiter-unavailable", "Unavailable.")])));
        return emailRequestLimiter;
    }

    private async Task RemoveProfileAsync(string identityUserId = "account")
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        await asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().Users.Where(user => user.IdentityUserId == identityUserId).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    private async Task SignInAsync(HttpClient? httpClient = null, string identityUserId = "account", string email = "user@example.com", string role = "User")
    {
        await CreateAccountAsync(identityUserId, email, role: role);
        using HttpResponseMessage response = await (httpClient ?? HttpClient).PostAsJsonAsync("/api/sessions", new { email, password = "Test-Password123!", rememberMe = false });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
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
