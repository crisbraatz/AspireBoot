using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspireBoot.Tests.Integration.Api.Identity.Auth.GetAntiforgery;

[Trait("Category", "Integration")]
public class GetAntiforgeryEndpointTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldReturnOkWhenProvidingATokenForAnonymousRequests()
    {
        using HttpClient httpClient = TestFixture.ApiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress(), false);

        using HttpResponseMessage response = await httpClient.GetAsync(new Uri("/api/auth/csrf", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument jsonJsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.False(string.IsNullOrWhiteSpace(jsonJsonDocument.RootElement.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task ShouldReturnOkWhenProvidingATokenForAnAuthenticatedSession()
    {
        await SignInAsync();

        using HttpResponseMessage response = await HttpClient.GetAsync(new Uri("/api/auth/csrf", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnTooManyRequestsWhenTheAuthRateLimitIsExceeded()
    {
        await using ApiWebApplicationFactory apiWebApplicationFactory = TestFixture.CreateApiWebApplicationFactory(new Dictionary<string, string?> { ["AspireBoot:Api:RateLimiters:Auth:PermitLimit"] = "1" });
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress(), false);
        using HttpResponseMessage firstResponse = await httpClient.GetAsync(new Uri("/api/auth/csrf", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        using HttpResponseMessage response = await httpClient.GetAsync(new Uri("/api/auth/csrf", UriKind.Relative), TestContext.Current.CancellationToken);

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

    private async Task SignInAsync(HttpClient? httpClient = null, string identityUserId = "account", string email = "user@example.com", string role = "User")
    {
        await CreateAccountAsync(identityUserId, email, role: role);
        using HttpResponseMessage response = await (httpClient ?? HttpClient).PostAsJsonAsync("/api/sessions", new { email, password = "Test-Password123!", rememberMe = false });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
