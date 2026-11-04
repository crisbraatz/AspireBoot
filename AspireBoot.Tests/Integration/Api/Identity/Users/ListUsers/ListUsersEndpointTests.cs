using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Integrations.ObjectStorage;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace AspireBoot.Tests.Integration.Api.Identity.Users.ListUsers;

[Trait("Category", "Integration")]
public class ListUsersEndpointTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldReturnOkWhenListingPeopleWithPagination()
    {
        User user = await SignInAsync();

        using HttpResponseMessage response = await HttpClient.GetAsync(new Uri("/api/users?page=1&size=10", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument bodyJsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, bodyJsonDocument.RootElement.GetProperty("count").GetInt32());
        Assert.Equal(user.Id, bodyJsonDocument.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.False(bodyJsonDocument.RootElement.GetProperty("items")[0].TryGetProperty("email", out _));
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheRequestIsUnauthenticated()
    {
        using HttpClient anonymousHttpClient = TestFixture.ApiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());

        using HttpResponseMessage response = await anonymousHttpClient.GetAsync(new Uri("/api/users", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    public async Task ShouldReturnBadRequestWhenThePaginationIsInvalid(int page, int size)
    {
        await SignInAsync();

        using HttpResponseMessage response = await HttpClient.GetAsync(new Uri($"/api/users?page={page}&size={size}", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldReturnOkWhenRestrictingInactiveProfilesToAdministrators(bool administrator)
    {
        User user = await CreateAccountAsync("target", "target@example.com");
        await SignInAsync(identityUserId: "viewer", email: "viewer@example.com", role: administrator ? "Administrator" : "User");
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        AspireBootDbContext aspireBootDbContext = asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>();
        (await aspireBootDbContext.Users.SingleAsync(itemUser => itemUser.Id == user.Id, TestContext.Current.CancellationToken)).Deactivate();
        await aspireBootDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await HttpClient.GetAsync(new Uri("/api/users", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument jsonJsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(administrator ? 2 : 1, jsonJsonDocument.RootElement.GetProperty("count").GetInt32());
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

        using HttpResponseMessage response = await httpClient.GetAsync(new Uri("/api/users", UriKind.Relative), TestContext.Current.CancellationToken);

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
