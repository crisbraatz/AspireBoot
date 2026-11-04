using System.Net;
using System.Net.Http.Json;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.Messaging;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Npgsql;

namespace AspireBoot.Tests.Integration.Api.Identity.Sessions.CreateSession;

[Trait("Category", "Integration")]
public class CreateSessionEndpointTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldReturnNoContentWhenIssuingAWorkingSessionCookie()
    {
        await CreateAccountAsync();

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/sessions", new { email = "user@example.com", password = "Test-Password123!", rememberMe = false }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith("__Host-AspireBoot.Session=", StringComparison.Ordinal));
        using HttpResponseMessage currentResponse = await HttpClient.GetAsync(new Uri("/api/sessions/current", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, currentResponse.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnBadRequestWhenLoginWithoutCsrfAreProvided()
    {
        await CreateAccountAsync();
        using HttpClient httpClient = TestFixture.ApiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress(), false);

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/sessions", new { email = "user@example.com", password = "Test-Password123!", rememberMe = false }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("identity.auth.csrf.invalid", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("invalid", "Test-Password123!")]
    [InlineData("user@example.com", "short")]
    public async Task ShouldReturnBadRequestWhenTheRequestIsInvalid(string address, string value)
    {
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/sessions", new { email = address, password = value, rememberMe = false }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnConflictWhenTheBrowserIsAlreadyAuthenticated()
    {
        await CreateAccountAsync();
        using HttpResponseMessage firstResponse = await HttpClient.PostAsJsonAsync("/api/sessions", new { email = "user@example.com", password = "Test-Password123!", rememberMe = false }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/sessions", new { email = "user@example.com", password = "Test-Password123!", rememberMe = false }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("identity.sessions.already-authenticated", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheAccountIsUnknown()
    {
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/sessions", new { email = "missing@example.com", password = "Test-Password123!", rememberMe = false }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("identity.auth.invalid-credentials", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheProfileIsMissing()
    {
        await CreateAccountAsync();
        await RemoveProfileAsync();

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/sessions", new { email = "user@example.com", password = "Test-Password123!", rememberMe = false }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("identity.auth.invalid-credentials", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheProfileIsInactive()
    {
        User user = await CreateAccountAsync();
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        AspireBootDbContext aspireBootDbContext = asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>();
        (await aspireBootDbContext.Users.SingleAsync(itemUser => itemUser.Id == user.Id, TestContext.Current.CancellationToken)).Deactivate();
        await aspireBootDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/sessions", new { email = "user@example.com", password = "Test-Password123!", rememberMe = false }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("identity.auth.invalid-credentials", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheCredentialsAreInvalid()
    {
        await CreateAccountAsync();

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/sessions", new { email = "user@example.com", password = "Wrong-Password123!", rememberMe = false }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheAccountIsLocked()
    {
        await CreateAccountAsync();
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        UserManager<IdentityUser> userManager = asyncServiceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.True((await userManager.SetLockoutEndDateAsync((await userManager.FindByIdAsync("account"))!, DateTimeOffset.UtcNow.AddMinutes(15))).Succeeded);

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/sessions", new { email = "user@example.com", password = "Test-Password123!", rememberMe = false }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("identity.auth.invalid-credentials", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenConfirmationIsRequired()
    {
        await CreateAccountAsync(isEmailConfirmed: false);
        await using ApiWebApplicationFactory apiWebApplicationFactory = TestFixture.CreateApiWebApplicationFactory(new Dictionary<string, string?>(), serviceCollection => serviceCollection.Configure<IdentityOptions>(identityOptions => identityOptions.SignIn.RequireConfirmedEmail = true));
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/sessions", new { email = "user@example.com", password = "Test-Password123!", rememberMe = false }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("identity.auth.invalid-credentials", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ShouldReturnUnauthorizedWhenTheOutboxFails(bool unavailable)
    {
        await CreateAccountAsync();
        await using ApiWebApplicationFactory apiWebApplicationFactory = CreateApiFactoryWithFailedOutbox(unavailable);
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/sessions", new { email = "user@example.com", password = "Test-Password123!", rememberMe = false }, TestContext.Current.CancellationToken);

        Assert.Equal(unavailable ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("private outbox details", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
        using HttpResponseMessage currentResponse = await httpClient.GetAsync(new Uri("/api/sessions/current", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, currentResponse.StatusCode);
    }

    private async Task<User> CreateAccountAsync(string identityUserId = "account", string email = "user@example.com", bool isEmailConfirmed = true, string role = "User")
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
        return profileUser;
    }

    private ApiWebApplicationFactory CreateApiFactoryWithFailedOutbox(bool unavailable)
    {
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        eventRepository.AddAsync(Arg.Any<IEvent>(), true, Arg.Any<CancellationToken>()).Returns(Task.FromException(unavailable ? new NpgsqlException("private outbox details", new TimeoutException()): new InvalidOperationException("private outbox details")));
        return TestFixture.CreateApiWebApplicationFactory(new Dictionary<string, string?>(), serviceCollection => serviceCollection.Replace(ServiceDescriptor.Singleton(eventRepository)));
    }

    private async Task RemoveProfileAsync(string identityUserId = "account")
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        await asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().Users.Where(user => user.IdentityUserId == identityUserId).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }
}
