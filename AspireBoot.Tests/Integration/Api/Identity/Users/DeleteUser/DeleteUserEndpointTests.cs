using System.Net;
using System.Net.Http.Json;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace AspireBoot.Tests.Integration.Api.Identity.Users.DeleteUser;

[Trait("Category", "Integration")]
public class DeleteUserEndpointTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldReturnNoContentWhenDeactivatingTheOwnedAccountAndRevokingItsSession()
    {
        User profileUser = await SignInAsync();
        using HttpRequestMessage request = new(HttpMethod.Delete, "/api/users/me");
        request.Content = JsonContent.Create(new { password = "Test-Password123!" });

        using HttpResponseMessage response = await HttpClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        Assert.False((await asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().Users.SingleAsync(user => user.Id == profileUser.Id, TestContext.Current.CancellationToken)).IsActive);
        using HttpResponseMessage currentResponse = await HttpClient.GetAsync(new Uri("/api/sessions/current", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, currentResponse.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheRequestIsUnauthenticated()
    {
        using HttpRequestMessage request = new(HttpMethod.Delete, "/api/users/me");
        request.Content = JsonContent.Create(new { password = "Test-Password123!" });

        using HttpResponseMessage response = await HttpClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenTheProfileDisappears()
    {
        await SignInAsync();
        await RemoveProfileAsync();
        using HttpRequestMessage request = new(HttpMethod.Delete, "/api/users/me");
        request.Content = JsonContent.Create(new { password = "Test-Password123!" });

        using HttpResponseMessage response = await HttpClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("identity.users.not-found", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenTheUserDisappearsAfterTheFilter()
    {
        User user = await CreateAccountAsync();
        await using ApiWebApplicationFactory apiWebApplicationFactory = CreateApiFactoryWithDeletionFailure(user, true);
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());
        await SignInAsync(httpClient);
        using HttpRequestMessage request = new(HttpMethod.Delete, "/api/users/me");
        request.Content = JsonContent.Create(new { password = "Test-Password123!" });

        using HttpResponseMessage response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        Assert.True((await asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().Users.SingleAsync(itemUser => itemUser.Id == user.Id, TestContext.Current.CancellationToken)).IsActive);
    }

    [Fact]
    public async Task ShouldReturnConflictWhenTheAccountChangesDuringDeletion()
    {
        User user = await CreateAccountAsync();
        await using ApiWebApplicationFactory apiWebApplicationFactory = CreateApiFactoryWithDeletionFailure(user, false);
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());
        await SignInAsync(httpClient);
        using HttpRequestMessage request = new(HttpMethod.Delete, "/api/users/me");
        request.Content = JsonContent.Create(new { password = "Test-Password123!" });

        using HttpResponseMessage response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        Assert.True((await asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().Users.SingleAsync(itemUser => itemUser.Id == user.Id, TestContext.Current.CancellationToken)).IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public async Task ShouldReturnBadRequestWhenAnInvalidPasswordRequestIsProvided(string password)
    {
        await SignInAsync();
        using HttpRequestMessage request = new(HttpMethod.Delete, "/api/users/me");
        request.Content = JsonContent.Create(new { password });

        using HttpResponseMessage response = await HttpClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenThePasswordIsIncorrect()
    {
        await SignInAsync();
        using HttpRequestMessage request = new(HttpMethod.Delete, "/api/users/me");
        request.Content = JsonContent.Create(new { password = "Wrong-Password123!" });

        using HttpResponseMessage response = await HttpClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

    private ApiWebApplicationFactory CreateApiFactoryWithDeletionFailure(User user, bool missingTarget)
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        userRepository.FindByIdAsync(user.Id, false, false, Arg.Any<CancellationToken>()).Returns(Task.FromResult(missingTarget ? null : user));
        userRepository.DeleteAsync(Arg.Any<User>(), Arg.Any<IdentityUser>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure" })));
        return TestFixture.CreateApiWebApplicationFactory(new Dictionary<string, string?>(), serviceCollection => serviceCollection.Replace(ServiceDescriptor.Singleton(userRepository)));
    }

    private async Task RemoveProfileAsync(string identityUserId = "account")
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        await asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().Users.Where(user => user.IdentityUserId == identityUserId).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    private async Task<User> SignInAsync(HttpClient? httpClient = null, string identityUserId = "account", string email = "user@example.com", string role = "User")
    {
        User user = await CreateAccountAsync(identityUserId, email, role: role);
        using HttpResponseMessage response = await (httpClient ?? HttpClient).PostAsJsonAsync("/api/sessions", new { email, password = "Test-Password123!", rememberMe = false });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return user;
    }
}
