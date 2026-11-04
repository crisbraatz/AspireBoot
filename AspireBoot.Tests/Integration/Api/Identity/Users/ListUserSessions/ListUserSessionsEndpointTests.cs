using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace AspireBoot.Tests.Integration.Api.Identity.Users.ListUserSessions;

[Trait("Category", "Integration")]
public class ListUserSessionsEndpointTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldReturnOnlyTheOwnersSessions()
    {
        User user = await SignInAsync();
        using HttpClient otherHttpClient = TestFixture.ApiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());
        await SignInAsync(otherHttpClient);

        using HttpResponseMessage response = await HttpClient.GetAsync(new Uri($"/api/users/{user.Id}/sessions?page=1&size=10", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument bodyJsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, bodyJsonDocument.RootElement.GetProperty("count").GetInt32());
        Assert.Single(bodyJsonDocument.RootElement.GetProperty("items").EnumerateArray(), itemJsonElement => itemJsonElement.TryGetProperty("isCurrent", out JsonElement currentJsonElement) && currentJsonElement.GetBoolean());
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheRequestIsUnauthenticated()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/users/00000000-0000-0000-0000-000000000001/sessions");

        using HttpResponseMessage response = await HttpClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenTheTargetDisappearsAfterTheFilter()
    {
        User user = await CreateAccountAsync();
        await using ApiWebApplicationFactory apiWebApplicationFactory = CreateApiFactoryWithMissingTarget(user);
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());
        await SignInAsync(httpClient);

        using HttpResponseMessage response = await httpClient.GetAsync(new Uri($"/api/users/{user.Id}/sessions", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("identity.users.not-found", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnAnEmptyPageAfterTheLastSession()
    {
        using HttpResponseMessage response = await HttpClient.GetAsync(new Uri($"/api/users/{(await SignInAsync()).Id}/sessions?page=2&size=100", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument jsonJsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, jsonJsonDocument.RootElement.GetProperty("count").GetInt32());
        Assert.Empty(jsonJsonDocument.RootElement.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheCurrentSessionIdIsMissing()
    {
        User user = await CreateAccountAsync();
        await using ApiWebApplicationFactory apiWebApplicationFactory = CreateApiFactoryWithoutCurrentSessionId();
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());
        await SignInAsync(httpClient);

        using HttpResponseMessage response = await httpClient.GetAsync(new Uri($"/api/users/{user.Id}/sessions", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("identity.auth.invalid-credentials", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Administrator")]
    public async Task ShouldReturnForbiddenWhenAnotherUsersSessionsEvenForAdministratorsIsProvided(string role)
    {
        User user = await CreateAccountAsync();
        await SignInAsync(identityUserId: "viewer", email: "viewer@example.com", role: role);

        using HttpResponseMessage response = await HttpClient.GetAsync(new Uri($"/api/users/{user.Id}/sessions", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    public async Task ShouldReturnBadRequestWhenThePaginationIsInvalid(int page, int size)
    {
        using HttpResponseMessage response = await HttpClient.GetAsync(new Uri($"/api/users/{(await SignInAsync()).Id}/sessions?page={page}&size={size}", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    private ApiWebApplicationFactory CreateApiFactoryWithMissingTarget(User user)
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), true, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        return TestFixture.CreateApiWebApplicationFactory(new Dictionary<string, string?>(), serviceCollection => serviceCollection.Replace(ServiceDescriptor.Singleton(userRepository)));
    }

    private ApiWebApplicationFactory CreateApiFactoryWithoutCurrentSessionId() => TestFixture.CreateApiWebApplicationFactory(new Dictionary<string, string?>(), serviceCollection => serviceCollection.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, cookieAuthenticationOptions =>
    {
        Func<CookieValidatePrincipalContext, Task> validatePrincipal = cookieAuthenticationOptions.Events.OnValidatePrincipal;
        cookieAuthenticationOptions.Events.OnValidatePrincipal = async cookieValidatePrincipalContext =>
        {
            await validatePrincipal(cookieValidatePrincipalContext);
            cookieValidatePrincipalContext.Properties.Items[Core.Identity.IdentityConstants.SessionIdProperty] = null;
        };
    }));

    private async Task<User> SignInAsync(HttpClient? httpClient = null, string identityUserId = "account", string email = "user@example.com", string role = "User")
    {
        User user = await CreateAccountAsync(identityUserId, email, role: role);
        using HttpResponseMessage response = await (httpClient ?? HttpClient).PostAsJsonAsync("/api/sessions", new { email, password = "Test-Password123!", rememberMe = false });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return user;
    }
}
