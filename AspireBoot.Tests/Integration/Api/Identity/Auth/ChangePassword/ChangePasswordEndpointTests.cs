using System.Net;
using System.Net.Http.Json;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspireBoot.Tests.Integration.Api.Identity.Auth.ChangePassword;

[Trait("Category", "Integration")]
public class ChangePasswordEndpointTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldReturnNoContentWhenChangingThePasswordAndEndingTheSession()
    {
        await SignInAsync();

        using HttpResponseMessage response = await HttpClient.PatchAsJsonAsync("/api/auth/password", new { currentPassword = "Test-Password123!", newPassword = "New-Password123!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using HttpResponseMessage currentResponse = await HttpClient.GetAsync(new Uri("/api/sessions/current", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, currentResponse.StatusCode);
        using HttpResponseMessage loginResponse = await HttpClient.PostAsJsonAsync("/api/sessions", new { email = "user@example.com", password = "New-Password123!", rememberMe = false }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheRequestIsUnauthenticated()
    {
        using HttpClient anonymousHttpClient = TestFixture.ApiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());

        using HttpResponseMessage response = await anonymousHttpClient.PatchAsJsonAsync("/api/auth/password", new { currentPassword = "Test-Password123!", newPassword = "New-Password123!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("", "New-Password123!")]
    [InlineData("short", "New-Password123!")]
    [InlineData("Test-Password123!", "short")]
    public async Task ShouldReturnBadRequestWhenTheRequestIsInvalid(string current, string replacement)
    {
        await SignInAsync();

        using HttpResponseMessage response = await HttpClient.PatchAsJsonAsync("/api/auth/password", new { currentPassword = current, newPassword = replacement }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnBadRequestWhenThePasswordIsUnchanged()
    {
        await SignInAsync();

        using HttpResponseMessage response = await HttpClient.PatchAsJsonAsync("/api/auth/password", new { currentPassword = "Test-Password123!", newPassword = "Test-Password123!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheProfileIsMissing()
    {
        await SignInAsync();
        await RemoveProfileAsync();

        using HttpResponseMessage response = await HttpClient.PatchAsJsonAsync("/api/auth/password", new { currentPassword = "Test-Password123!", newPassword = "New-Password123!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("identity.auth.invalid-credentials", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheCurrentPasswordIsIncorrect()
    {
        await SignInAsync();

        using HttpResponseMessage response = await HttpClient.PatchAsJsonAsync("/api/auth/password", new { currentPassword = "Wrong-Password123!", newPassword = "New-Password123!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnIdentityPasswordPolicyFailures()
    {
        await SignInAsync();

        using HttpResponseMessage response = await HttpClient.PatchAsJsonAsync("/api/auth/password", new { currentPassword = "Test-Password123!", newPassword = "weak-password123!" }, TestContext.Current.CancellationToken);

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
}
