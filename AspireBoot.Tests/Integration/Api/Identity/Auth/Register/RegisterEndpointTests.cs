using System.Net;
using System.Net.Http.Json;
using System.Text;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspireBoot.Tests.Integration.Api.Identity.Auth.Register;

[Trait("Category", "Integration")]
public class RegisterEndpointTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldReturnAcceptedWhenCreatingAnAccountAndItsProfile()
    {
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/register", new { email = "USER@EXAMPLE.COM", password = "Test-Password123!", name = "  uSER nAME  " }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        User user = await asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().Users.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("user@example.com", user.Email);
        Assert.Equal("User Name", user.Name);
    }

    [Fact]
    public async Task ShouldReturnAcceptedWhenTheEmailAlreadyExists()
    {
        await CreateAccountAsync();

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/register", new { email = "user@example.com", password = "Test-Password123!", name = "User Name" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Theory]
    [InlineData("{", "application/json", 400)]
    [InlineData("plain text", "text/plain", 415)]
    public async Task ShouldReturnBadRequestOrUnsupportedMediaTypeWhenTheBodyIsMalformedOrUnsupported(string body, string contentType, int statusCode)
    {
        using StringContent stringContent = new(body, Encoding.UTF8, contentType);

        using HttpResponseMessage response = await HttpClient.PostAsync(new Uri("/api/auth/register", UriKind.Relative), stringContent, TestContext.Current.CancellationToken);

        Assert.Equal((HttpStatusCode)statusCode, response.StatusCode);
    }

    [Theory]
    [InlineData("invalid", "Test-Password123!", "User Name")]
    [InlineData("user@example.com", "short", "User Name")]
    [InlineData("user@example.com", "Test-Password123!", " a ")]
    [InlineData("user@example.com", "Test-Password123!", "User123")]
    public async Task ShouldReturnBadRequestWhenTheRegistrationIsInvalid(string email, string password, string name)
    {
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/register", new { email, password, name }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(" a ")]
    [InlineData(" ́́ ")]
    public async Task ShouldReturnBadRequestWhenNamesThatAreInvalidAfterNormalizationAreProvided(string invalidName)
    {
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/register", new { email = "user@example.com", password = "Test-Password123!", name = invalidName }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("identity.auth.name.", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("test-password123!")]
    [InlineData("TEST-PASSWORD123!")]
    [InlineData("Test-Password!!!!")]
    [InlineData("TestPassword12345")]
    public async Task ShouldReturnIdentityPasswordPolicyFailures(string invalidPassword)
    {
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync("/api/auth/register", new { email = "user@example.com", password = invalidPassword, name = "User Name" }, TestContext.Current.CancellationToken);

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
}
