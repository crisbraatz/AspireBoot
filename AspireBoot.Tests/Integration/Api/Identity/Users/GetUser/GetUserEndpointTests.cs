using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Identity.Users.GetUser;
using AspireBoot.Core.Integrations.Caching;
using AspireBoot.Core.Integrations.ObjectStorage;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace AspireBoot.Tests.Integration.Api.Identity.Users.GetUser;

[Trait("Category", "Integration")]
public class GetUserEndpointTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task ShouldReturnOkWhenProjectingEmailAccordingToTheViewer(bool ownsProfile, bool administrator)
    {
        User user = await CreateAccountAsync();
        if (ownsProfile)
            await SignInAsync();
        else
            await SignInAsync(identityUserId: "viewer", email: "viewer@example.com", role: administrator ? "Administrator" : "User");

        using HttpResponseMessage response = await HttpClient.GetAsync(new Uri($"/api/users/{user.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument bodyJsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(ownsProfile || administrator, bodyJsonDocument.RootElement.TryGetProperty("email", out _));
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheRequestIsUnauthenticated()
    {
        using HttpClient anonymousHttpClient = TestFixture.ApiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());

        using HttpResponseMessage response = await anonymousHttpClient.GetAsync(new Uri("/api/users/00000000-0000-0000-0000-000000000001", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnNotFoundAndRecheckActiveStateBeforeReturningCachedData()
    {
        User user = await CreateAccountAsync("target", "target@example.com");
        await SignInAsync(identityUserId: "viewer", email: "viewer@example.com");
        using HttpResponseMessage firstResponse = await HttpClient.GetAsync(new Uri($"/api/users/{user.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        AspireBootDbContext aspireBootDbContext = asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>();
        (await aspireBootDbContext.Users.SingleAsync(itemUser => itemUser.Id == user.Id, TestContext.Current.CancellationToken)).Deactivate();
        await aspireBootDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await HttpClient.GetAsync(new Uri($"/api/users/{user.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("identity.users.not-found", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("null")]
    [InlineData("wrong-user")]
    public async Task ShouldReturnOkWhenDiscardInvalidOrMismatchedCachedProfiles(string state)
    {
        User user = await SignInAsync();
        string cached = state switch { "malformed" => "not-json", "null" => "null", _ => JsonSerializer.Serialize(new CachedUser(Guid.NewGuid(), "private-other@example.com", "Other Name")) };
        await using AsyncServiceScope setupScopeAsyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        Assert.True((await setupScopeAsyncServiceScope.ServiceProvider.GetRequiredService<ICache>().SetAsync($"identity:users:{user.Id:N}", cached, TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken)).IsSuccess);

        using HttpResponseMessage response = await HttpClient.GetAsync(new Uri($"/api/users/{user.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument jsonJsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(user.Id, jsonJsonDocument.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(user.Name, jsonJsonDocument.RootElement.GetProperty("name").GetString());
        Assert.Equal(user.Email, jsonJsonDocument.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task ShouldReturnOkWhenTheOptionalCacheIsUnavailable()
    {
        User user = await CreateAccountAsync();
        await using ApiWebApplicationFactory apiWebApplicationFactory = CreateApiFactoryWithUnavailableCache();
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());
        await SignInAsync(httpClient);

        using HttpResponseMessage response = await httpClient.GetAsync(new Uri($"/api/users/{user.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument jsonJsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(user.Name, jsonJsonDocument.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task ShouldReturnNotFoundForAnUnknownUser()
    {
        await SignInAsync();

        using HttpResponseMessage response = await HttpClient.GetAsync(new Uri($"/api/users/{Guid.NewGuid()}", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldReturnExpectedStatusCodeWhenRestrictingInactiveProfilesToAdministrators(bool administrator)
    {
        User user = await CreateAccountAsync("target", "target@example.com");
        await SignInAsync(identityUserId: "viewer", email: "viewer@example.com", role: administrator ? "Administrator" : "User");
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        AspireBootDbContext aspireBootDbContext = asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>();
        (await aspireBootDbContext.Users.SingleAsync(itemUser => itemUser.Id == user.Id, TestContext.Current.CancellationToken)).Deactivate();
        await aspireBootDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await HttpClient.GetAsync(new Uri($"/api/users/{user.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(administrator ? HttpStatusCode.OK : HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnServiceUnavailableWhenPropagatingAnAvatarSigningFailure()
    {
        User user = await CreateAccountAsync();
        await using (AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope())
        {
            AspireBootDbContext aspireBootDbContext = asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>();
            User storedUser = await aspireBootDbContext.Users.SingleAsync(itemUser => itemUser.Id == user.Id, TestContext.Current.CancellationToken);
            storedUser.ChangeAvatarKey("avatar");
            await aspireBootDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using ApiWebApplicationFactory apiWebApplicationFactory = CreateApiFactoryWithUnavailableStorage();
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());
        await SignInAsync(httpClient);

        using HttpResponseMessage response = await httpClient.GetAsync(new Uri($"/api/users/{user.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("storage.unavailable", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
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

    private ApiWebApplicationFactory CreateApiFactoryWithUnavailableCache()
    {
        ICache cache = Substitute.For<ICache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string?>.Failure([Failure.ServiceUnavailable("cache.unavailable", "Unavailable.")])));
        cache.SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        return TestFixture.CreateApiWebApplicationFactory(new Dictionary<string, string?>(), serviceCollection => serviceCollection.Replace(ServiceDescriptor.Singleton(cache)));
    }

    private ApiWebApplicationFactory CreateApiFactoryWithUnavailableStorage()
    {
        IObjectStorage objectStorage = Substitute.For<IObjectStorage>();
        objectStorage.CreateUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string>.Failure([Failure.ServiceUnavailable("storage.unavailable", "Unavailable.")])));
        return TestFixture.CreateApiWebApplicationFactory(new Dictionary<string, string?>(), serviceCollection => serviceCollection.Replace(ServiceDescriptor.Singleton(objectStorage)));
    }

    private async Task<User> SignInAsync(HttpClient? httpClient = null, string identityUserId = "account", string email = "user@example.com", string role = "User")
    {
        User user = await CreateAccountAsync(identityUserId, email, role: role);
        using HttpResponseMessage response = await (httpClient ?? HttpClient).PostAsJsonAsync("/api/sessions", new { email, password = "Test-Password123!", rememberMe = false });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return user;
    }
}
