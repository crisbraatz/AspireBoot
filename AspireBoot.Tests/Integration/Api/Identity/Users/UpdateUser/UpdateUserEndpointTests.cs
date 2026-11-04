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

namespace AspireBoot.Tests.Integration.Api.Identity.Users.UpdateUser;

[Trait("Category", "Integration")]
public class UpdateUserEndpointTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldReturnNoContentAndUpdateTheOwnersNameAndAvatar()
    {
        User user = await SignInAsync();
        using MultipartFormDataContent formMultipartFormDataContent = new();
        using StringContent nameStringContent = new("Updated Name");
        formMultipartFormDataContent.Add(nameStringContent, "newName");
        using ByteArrayContent imageByteArrayContent = new([0xFF, 0xD8, 0xFF]);
        imageByteArrayContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        formMultipartFormDataContent.Add(imageByteArrayContent, "avatar", "avatar.jpg");

        using HttpResponseMessage response = await HttpClient.PatchAsync(new Uri("/api/users/me", UriKind.Relative), formMultipartFormDataContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using HttpResponseMessage detailsResponse = await HttpClient.GetAsync(new Uri($"/api/users/{user.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        using JsonDocument bodyJsonDocument = JsonDocument.Parse(await detailsResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Updated Name", bodyJsonDocument.RootElement.GetProperty("name").GetString());
        Assert.False(string.IsNullOrWhiteSpace(bodyJsonDocument.RootElement.GetProperty("avatarUrl").GetString()));
    }

    [Fact]
    public async Task ShouldReturnNoContentAndUpdateOnlyTheName()
    {
        User user = await SignInAsync();
        using MultipartFormDataContent formMultipartFormDataContent = new();
        using StringContent nameStringContent = new("Updated Name");
        formMultipartFormDataContent.Add(nameStringContent, "newName");

        using HttpResponseMessage response = await HttpClient.PatchAsync(new Uri("/api/users/me", UriKind.Relative), formMultipartFormDataContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        User storedUser = await asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().Users.SingleAsync(itemUser => itemUser.Id == user.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Updated Name", storedUser.Name);
        Assert.Null(storedUser.AvatarKey);
    }

    [Theory]
    [InlineData("image/jpeg", "FFD8FF", ".jpg")]
    [InlineData("image/png", "89504E470D0A1A0A", ".png")]
    [InlineData("image/webp", "524946463030303057454250", ".webp")]
    public async Task ShouldReturnNoContentWhenAcceptingEachSupportedAvatarFormat(string contentType, string signature, string extension)
    {
        User user = await SignInAsync();
        using MultipartFormDataContent formMultipartFormDataContent = new();
        using ByteArrayContent imageByteArrayContent = new(Convert.FromHexString(signature));
        imageByteArrayContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        formMultipartFormDataContent.Add(imageByteArrayContent, "avatar", "avatar" + extension);

        using HttpResponseMessage response = await HttpClient.PatchAsync(new Uri("/api/users/me", UriKind.Relative), formMultipartFormDataContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        Assert.EndsWith(extension, (await asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().Users.SingleAsync(itemUser => itemUser.Id == user.Id, TestContext.Current.CancellationToken)).AvatarKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheRequestIsUnauthenticated()
    {
        using HttpRequestMessage request = new(HttpMethod.Patch, "/api/users/me");
        request.Content = new MultipartFormDataContent();

        using HttpResponseMessage response = await HttpClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(1, 400, "identity.users.avatar.maximum-size")]
    [InlineData(131072, 413, "Payload too large")]
    public async Task ShouldReturnExpectedStatusCodeAndEnforceTheAvatarAndRequestBodySizeLimits(int extraBytes, int statusCode, string detail)
    {
        await SignInAsync();
        byte[] bytes = new byte[4 * 1024 * 1024 + extraBytes];
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        using MultipartFormDataContent formMultipartFormDataContent = new();
        using ByteArrayContent imageByteArrayContent = new(bytes);
        imageByteArrayContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        formMultipartFormDataContent.Add(imageByteArrayContent, "avatar", "avatar.jpg");

        using HttpResponseMessage response = await HttpClient.PatchAsync(new Uri("/api/users/me", UriKind.Relative), formMultipartFormDataContent, TestContext.Current.CancellationToken);

        Assert.Equal((HttpStatusCode)statusCode, response.StatusCode);
        Assert.Contains(detail, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenTheProfileDisappears()
    {
        await SignInAsync();
        await RemoveProfileAsync();
        using MultipartFormDataContent formMultipartFormDataContent = new();
        using StringContent nameStringContent = new("Updated Name");
        formMultipartFormDataContent.Add(nameStringContent, "newName");

        using HttpResponseMessage response = await HttpClient.PatchAsync(new Uri("/api/users/me", UriKind.Relative), formMultipartFormDataContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("identity.users.not-found", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenTheTargetDisappearsAfterTheFilter()
    {
        await using ApiWebApplicationFactory apiWebApplicationFactory = CreateApiFactoryWithMissingTarget(await CreateAccountAsync());
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());
        await SignInAsync(httpClient);
        using MultipartFormDataContent formMultipartFormDataContent = new();
        using StringContent nameStringContent = new("Updated Name");
        formMultipartFormDataContent.Add(nameStringContent, "newName");

        using HttpResponseMessage response = await httpClient.PatchAsync(new Uri("/api/users/me", UriKind.Relative), formMultipartFormDataContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("identity.users.not-found", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnNoContentAndLeaveAnUnchangedProfileWithoutAnAudit()
    {
        User user = await SignInAsync();
        using MultipartFormDataContent formMultipartFormDataContent = new();
        using StringContent nameStringContent = new("  USER NAME  ");
        formMultipartFormDataContent.Add(nameStringContent, "newName");

        using HttpResponseMessage response = await HttpClient.PatchAsync(new Uri("/api/users/me", UriKind.Relative), formMultipartFormDataContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        AspireBootDbContext aspireBootDbContext = asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>();
        Assert.False(await aspireBootDbContext.UserAudits.AnyAsync(itemUserAudit => itemUserAudit.UserId == user.Id, TestContext.Current.CancellationToken));
        Assert.Null((await aspireBootDbContext.Users.SingleAsync(itemUser => itemUser.Id == user.Id, TestContext.Current.CancellationToken)).UpdatedAtUtc);
    }

    [Fact]
    public async Task ShouldReturnBadRequestWhenAnInvalidNameIsProvided()
    {
        await SignInAsync();
        using MultipartFormDataContent formMultipartFormDataContent = new();
        using StringContent nameStringContent = new("User123");
        formMultipartFormDataContent.Add(nameStringContent, "newName");

        using HttpResponseMessage response = await HttpClient.PatchAsync(new Uri("/api/users/me", UriKind.Relative), formMultipartFormDataContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ShouldReturnBadRequestWhenTheUpdateIsEmpty(bool unknownFields)
    {
        await SignInAsync();
        using MultipartFormDataContent formMultipartFormDataContent = new();
        using StringContent ignoredStringContent = new("ignored");
        if (unknownFields)
            formMultipartFormDataContent.Add(ignoredStringContent, "unused");

        using HttpResponseMessage response = await HttpClient.PatchAsync(new Uri("/api/users/me", UriKind.Relative), formMultipartFormDataContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (unknownFields)
            Assert.Contains("identity.users.empty", body, StringComparison.Ordinal);
        using JsonDocument jsonJsonDocument = JsonDocument.Parse(body);
        Assert.Equal(400, jsonJsonDocument.RootElement.GetProperty("status").GetInt32());
    }

    [Theory]
    [InlineData(" a ")]
    [InlineData(" ́́ ")]
    public async Task ShouldReturnBadRequestWhenANameThatFailsCoreValidationIsProvided(string value)
    {
        await SignInAsync();
        using MultipartFormDataContent formMultipartFormDataContent = new();
        using StringContent nameStringContent = new(value);
        formMultipartFormDataContent.Add(nameStringContent, "newName");

        using HttpResponseMessage response = await HttpClient.PatchAsync(new Uri("/api/users/me", UriKind.Relative), formMultipartFormDataContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("identity.users.name.", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnBadRequestWhenAnInvalidAvatarIsProvided()
    {
        await SignInAsync();
        using MultipartFormDataContent formMultipartFormDataContent = new();
        using ByteArrayContent imageByteArrayContent = new([1, 2, 3]);
        imageByteArrayContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        formMultipartFormDataContent.Add(imageByteArrayContent, "avatar", "avatar.jpg");

        using HttpResponseMessage response = await HttpClient.PatchAsync(new Uri("/api/users/me", UriKind.Relative), formMultipartFormDataContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnBadRequestWhenAnUnsupportedAvatarTypeIsProvided()
    {
        await SignInAsync();
        using MultipartFormDataContent formMultipartFormDataContent = new();
        using ByteArrayContent imageByteArrayContent = new([0xFF, 0xD8, 0xFF]);
        imageByteArrayContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/gif");
        formMultipartFormDataContent.Add(imageByteArrayContent, "avatar", "avatar.gif");

        using HttpResponseMessage response = await HttpClient.PatchAsync(new Uri("/api/users/me", UriKind.Relative), formMultipartFormDataContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("identity.users.avatar.invalid", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReturnServiceUnavailableWhenUploadFails()
    {
        User user = await CreateAccountAsync();
        await using (AsyncServiceScope asyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope())
        {
            AspireBootDbContext aspireBootDbContext = asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>();
            User storedUser = await aspireBootDbContext.Users.SingleAsync(itemUser => itemUser.Id == user.Id, TestContext.Current.CancellationToken);
            storedUser.ChangeAvatarKey("old-avatar");
            await aspireBootDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using ApiWebApplicationFactory apiWebApplicationFactory = CreateApiFactoryWithFailedUpload();
        using HttpClient httpClient = apiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());
        await SignInAsync(httpClient);
        using MultipartFormDataContent formMultipartFormDataContent = new();
        using ByteArrayContent imageByteArrayContent = new([0xFF, 0xD8, 0xFF]);
        imageByteArrayContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        formMultipartFormDataContent.Add(imageByteArrayContent, "avatar", "avatar.jpg");

        using HttpResponseMessage response = await httpClient.PatchAsync(new Uri("/api/users/me", UriKind.Relative), formMultipartFormDataContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("storage.unavailable", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
        await using AsyncServiceScope verificationAsyncServiceScope = TestFixture.ApiWebApplicationFactory.Services.CreateAsyncScope();
        Assert.Equal("old-avatar", (await verificationAsyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>().Users.SingleAsync(itemUser => itemUser.Id == user.Id, TestContext.Current.CancellationToken)).AvatarKey);
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

    private ApiWebApplicationFactory CreateApiFactoryWithFailedUpload()
    {
        IObjectStorage objectStorage = Substitute.For<IObjectStorage>();
        objectStorage.PutAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.ServiceUnavailable("storage.unavailable", "Unavailable.")])));
        return TestFixture.CreateApiWebApplicationFactory(new Dictionary<string, string?>(), serviceCollection => serviceCollection.Replace(ServiceDescriptor.Singleton(objectStorage)));
    }

    private ApiWebApplicationFactory CreateApiFactoryWithMissingTarget(User user)
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), true, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
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
