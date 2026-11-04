using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspireBoot.Tests.Integration.Api.Identity.Sessions.DeleteSession;

[Trait("Category", "Integration")]
public class DeleteSessionEndpointTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldReturnNoContentWhenRevokingAnOwnedSession()
    {
        User user = await SignInAsync();
        using HttpClient otherHttpClient = TestFixture.ApiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());
        await SignInAsync(otherHttpClient);

        using HttpResponseMessage response = await HttpClient.DeleteAsync(new Uri($"/api/sessions/{await GetCurrentSessionIdAsync(otherHttpClient, user.Id)}", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using HttpResponseMessage revokedResponse = await otherHttpClient.GetAsync(new Uri("/api/sessions/current", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, revokedResponse.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheRequestIsUnauthenticated()
    {
        using HttpRequestMessage request = new(HttpMethod.Delete, "/api/sessions/unknown");

        using HttpResponseMessage response = await HttpClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnNotFoundAndNotRevokeAnotherAccountsSession()
    {
        await SignInAsync();
        using HttpClient otherHttpClient = TestFixture.ApiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());

        using HttpResponseMessage response = await HttpClient.DeleteAsync(new Uri($"/api/sessions/{await GetCurrentSessionIdAsync(otherHttpClient, (await SignInAsync(otherHttpClient, "other", "other@example.com")).Id)}", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using HttpResponseMessage currentResponse = await otherHttpClient.GetAsync(new Uri("/api/sessions/current", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, currentResponse.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnNotFoundForAnUnknownSession()
    {
        await SignInAsync();

        using HttpResponseMessage response = await HttpClient.DeleteAsync(new Uri("/api/sessions/missing", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("identity.sessions.not-found", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
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

    private static async Task<string> GetCurrentSessionIdAsync(HttpClient httpClient, Guid userId)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(new Uri($"/api/users/{userId}/sessions", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        using JsonDocument bodyJsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return bodyJsonDocument.RootElement.GetProperty("items").EnumerateArray().Single(itemJsonElement => itemJsonElement.TryGetProperty("isCurrent", out JsonElement currentJsonElement) && currentJsonElement.GetBoolean()).GetProperty("id").GetString()!;
    }

    private async Task<User> SignInAsync(HttpClient? httpClient = null, string identityUserId = "account", string email = "user@example.com", string role = "User")
    {
        User user = await CreateAccountAsync(identityUserId, email, role: role);
        using HttpResponseMessage response = await (httpClient ?? HttpClient).PostAsJsonAsync("/api/sessions", new { email, password = "Test-Password123!", rememberMe = false });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return user;
    }
}
