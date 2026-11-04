using System.Net;
using System.Net.Http.Json;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspireBoot.Tests.Integration.Api.Identity.Auth.ResetPassword;

[Trait("Category", "Integration")]
public class ResetPasswordEndpointTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldReturnNoContentWhenReplacingTheAccountsPassword()
    {
        await CreateAccountAsync();

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/reset-password", new { email = "user@example.com", password = "New-Password123!", token = await CreateTokenAsync() }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        UserManager<IdentityUser> userManager = asyncServiceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        IdentityUser? identityUser = await userManager.FindByIdAsync("account");
        Assert.True(await userManager.CheckPasswordAsync(identityUser!, "New-Password123!"));
    }

    [Fact]
    public async Task ShouldReturnNoContentWhenRevokingEveryExistingSessionAfterChangingCredentials()
    {
        await SignInAsync();
        using HttpClient otherHttpClient = TestFixture.ApiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());
        await SignInAsync(otherHttpClient);

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/reset-password", new { email = "user@example.com", password = "New-Password123!", token = await CreateTokenAsync() }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using HttpResponseMessage currentResponse = await HttpClient.GetAsync(new Uri("/api/sessions/current", UriKind.Relative), TestContext.Current.CancellationToken);
        using HttpResponseMessage revokedResponse = await otherHttpClient.GetAsync(new Uri("/api/sessions/current", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, currentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, revokedResponse.StatusCode);
    }

    [Theory]
    [InlineData("invalid", "New-Password123!", "token")]
    [InlineData("user@example.com", "short", "token")]
    [InlineData("user@example.com", "New-Password123!", "")]
    public async Task ShouldReturnBadRequestWhenTheRequestIsInvalid(string address, string value, string requestToken)
    {
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/reset-password", new { email = address, password = value, token = requestToken }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheAccountIsUnknown()
    {
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/reset-password", new { email = "missing@example.com", password = "New-Password123!", token = "invalid-token" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("identity.auth.invalid-credentials", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheProfileIsMissing()
    {
        await CreateAccountAsync();
        string token = await CreateTokenAsync();
        await RemoveProfileAsync();

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/reset-password", new { email = "user@example.com", password = "New-Password123!", token }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("identity.auth.invalid-credentials", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnBadRequestWhenTheTokenIsInvalid()
    {
        await CreateAccountAsync();

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/reset-password", new { email = "user@example.com", password = "New-Password123!", token = "invalid-token" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnBadRequestWhenTheTokenHasExpired()
    {
        await CreateAccountAsync(isEmailConfirmed: true);
        string token = await CreateTokenAsync();
        await using ApiWebApplicationFactory apiWebApplicationFactory = TestFixture.CreateApiWebApplicationFactory(new Dictionary<string, string?>(), servicesServiceCollection => servicesServiceCollection.Configure<DataProtectionTokenProviderOptions>(dataProtectionTokenProviderOptions => dataProtectionTokenProviderOptions.TokenLifespan = TimeSpan.Zero));
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/auth/reset-password", new { email = "user@example.com", password = "New-Password123!", token }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("identity.auth.InvalidToken", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnIdentityPasswordPolicyFailures()
    {
        await CreateAccountAsync();

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/reset-password", new { email = "user@example.com", password = "weak-password123!", token = await CreateTokenAsync() }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("identity.auth.Password", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
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

    private async Task<string> CreateTokenAsync()
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        UserManager<IdentityUser> userManager = asyncServiceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        return await userManager.GeneratePasswordResetTokenAsync((await userManager.FindByIdAsync("account"))!);
    }

    private async Task RemoveProfileAsync(string identityUserId = "account")
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        await asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().Users.Where(user => user.IdentityUserId == identityUserId).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    private async Task SignInAsync(HttpClient? httpClient = null, string identityUserId = "account", string email = "user@example.com", string role = "User")
    {
        await CreateAccountAsync(identityUserId, email, role: role);
        using HttpResponseMessage response = await (httpClient ?? HttpClient).PostAsJsonAsync("/api/sessions", new { email, password = "Test-Password123!", rememberMe = false }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
