using System.Net;
using System.Net.Http.Json;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspireBoot.Tests.Integration.Api.Identity.Auth.ConfirmEmail;

[Trait("Category", "Integration")]
public class ConfirmEmailEndpointTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldReturnNoContentWhenConfirmingTheAccountsEmail()
    {
        await CreateAccountAsync(isEmailConfirmed: false);

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/confirm-email", new { identityUserId = "account", token = await CreateTokenAsync() }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        IdentityUser? identityUser = await asyncServiceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().FindByIdAsync("account");
        Assert.True(identityUser!.EmailConfirmed);
    }

    [Fact]
    public async Task ShouldReturnNoContentWhenAllowingAnAlreadyConfirmedAccountWithoutASecondWelcomeRequest()
    {
        await CreateAccountAsync(isEmailConfirmed: false);
        string token = await CreateTokenAsync();
        using HttpResponseMessage firstResponse = await HttpClient.PostAsJsonAsync("/api/auth/confirm-email", new { identityUserId = "account", token }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/confirm-email", new { identityUserId = "account", token }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData("", "token")]
    [InlineData("account", "")]
    public async Task ShouldReturnBadRequestWhenTheRequestIsInvalid(string identityUserId, string value)
    {
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/confirm-email", new { identityUserId, token = value }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheAccountIsUnknown()
    {
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/confirm-email", new { identityUserId = "missing", token = "invalid-token" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("identity.auth.invalid-credentials", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheProfileIsMissing()
    {
        await CreateAccountAsync(isEmailConfirmed: false);
        string token = await CreateTokenAsync();
        await RemoveProfileAsync();

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/confirm-email", new { identityUserId = "account", token }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("identity.auth.invalid-credentials", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnBadRequestWhenTheTokenIsInvalid()
    {
        await CreateAccountAsync(isEmailConfirmed: false);

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/confirm-email", new { identityUserId = "account", token = "invalid-token" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnBadRequestWhenTheTokenHasExpired()
    {
        await CreateAccountAsync(isEmailConfirmed: false);
        string token = await CreateTokenAsync();
        await using ApiWebApplicationFactory apiWebApplicationFactory = TestFixture.CreateApiWebApplicationFactory(new Dictionary<string, string?>(), servicesServiceCollection => servicesServiceCollection.Configure<DataProtectionTokenProviderOptions>(dataProtectionTokenProviderOptions => dataProtectionTokenProviderOptions.TokenLifespan = TimeSpan.Zero));
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/auth/confirm-email", new { identityUserId = "account", token }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("identity.auth.InvalidToken", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
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
        return await userManager.GenerateEmailConfirmationTokenAsync((await userManager.FindByIdAsync("account"))!);
    }

    private async Task RemoveProfileAsync(string identityUserId = "account")
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        await asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().Users.Where(user => user.IdentityUserId == identityUserId).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }
}
