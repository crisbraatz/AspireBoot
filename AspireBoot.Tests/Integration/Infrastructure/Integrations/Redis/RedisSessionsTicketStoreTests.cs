using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using AspireBoot.Core.Identity;
using AspireBoot.Core.Identity.Sessions;
using AspireBoot.Infrastructure.Integrations.Redis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AspireBoot.Tests.Integration.Infrastructure.Integrations.Redis;

[Trait("Category", "Integration")]
public class RedisSessionsTicketStoreTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ListAsyncShouldReturnTheCurrentSessionFirstWithItsTrackedDetails()
    {
        RedisSessionsTicketStore redisSessionsTicketStore = CreateRedisSessionsTicketStore();
        TrackedDetails trackedDetails = new("Firefox", "Linux", "Desktop", "127.0.0.1");
        AuthenticationTicket authenticationTicket = CreateAuthenticationTicket("account");
        authenticationTicket.Properties.Items[IdentityConstants.SessionTrackedDetailsProperty] = JsonSerializer.Serialize(trackedDetails);
        string currentSessionId = await redisSessionsTicketStore.StoreAsync(authenticationTicket);
        string otherSessionId = await redisSessionsTicketStore.StoreAsync(CreateAuthenticationTicket("account"));

        IReadOnlyList<IssuedSession> issuedSessions = await redisSessionsTicketStore.ListAsync("account", currentSessionId, TestContext.Current.CancellationToken);

        Assert.Equal(2, issuedSessions.Count);
        Assert.Equal(currentSessionId, issuedSessions[0].Id);
        Assert.True(issuedSessions[0].IsCurrent);
        Assert.Equal(trackedDetails, issuedSessions[0].TrackedDetails);
        Assert.Equal(otherSessionId, issuedSessions[1].Id);
        Assert.Null(issuedSessions[1].IsCurrent);
        Assert.Null(issuedSessions[1].TrackedDetails);
    }

    [Fact]
    public async Task ListAsyncShouldReturnOnlyValidAccountSessionsWhenRemovingStaleAndForeignIndexEntries()
    {
        RedisSessionsTicketStore redisSessionsTicketStore = CreateRedisSessionsTicketStore();
        string foreignSessionId = await redisSessionsTicketStore.StoreAsync(CreateAuthenticationTicket("other-account"));
        await GetConnectionMultiplexer().GetDatabase().SetAddAsync(IdentityConstants.GetAuthSessionsKey("account"), ["missing", foreignSessionId]);

        IReadOnlyList<IssuedSession> issuedSessions = await redisSessionsTicketStore.ListAsync("account", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(issuedSessions);
        Assert.Equal(0, await GetConnectionMultiplexer().GetDatabase().SetLengthAsync(IdentityConstants.GetAuthSessionsKey("account")));
        Assert.NotNull(await redisSessionsTicketStore.RetrieveAsync(foreignSessionId));
    }

    [Fact]
    public async Task RemoveAsyncShouldRemoveTheTicketAndItsAccountIndexEntry()
    {
        RedisSessionsTicketStore redisSessionsTicketStore = CreateRedisSessionsTicketStore();
        string sessionId = await redisSessionsTicketStore.StoreAsync(CreateAuthenticationTicket("account"));

        await redisSessionsTicketStore.RemoveAsync(sessionId);
        await redisSessionsTicketStore.RemoveAsync(sessionId);

        Assert.Null(await redisSessionsTicketStore.RetrieveAsync(sessionId));
        Assert.Empty(await redisSessionsTicketStore.ListAsync("account", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RemoveOthersAsyncShouldRemoveOnlyTheOtherSessionsOfTheRequestedAccount()
    {
        RedisSessionsTicketStore redisSessionsTicketStore = CreateRedisSessionsTicketStore();
        string currentSessionId = await redisSessionsTicketStore.StoreAsync(CreateAuthenticationTicket("account"));
        string otherSessionId = await redisSessionsTicketStore.StoreAsync(CreateAuthenticationTicket("account"));
        string foreignSessionId = await redisSessionsTicketStore.StoreAsync(CreateAuthenticationTicket("other-account"));

        await redisSessionsTicketStore.RemoveOthersAsync("account", currentSessionId, TestContext.Current.CancellationToken);

        Assert.NotNull(await redisSessionsTicketStore.RetrieveAsync(currentSessionId));
        Assert.Null(await redisSessionsTicketStore.RetrieveAsync(otherSessionId));
        Assert.NotNull(await redisSessionsTicketStore.RetrieveAsync(foreignSessionId));
        Assert.Equal(currentSessionId, Assert.Single(await redisSessionsTicketStore.ListAsync("account", cancellationToken: TestContext.Current.CancellationToken)).Id);
    }

    [Fact]
    public async Task RenewAsyncShouldDeleteTheSessionWhenNoLifetimeRemains()
    {
        RedisSessionsTicketStore redisSessionsTicketStore = CreateRedisSessionsTicketStore();
        string sessionId = await redisSessionsTicketStore.StoreAsync(CreateAuthenticationTicket("account"));
        AuthenticationTicket authenticationTicket = CreateAuthenticationTicket("account");
        authenticationTicket.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1);

        await redisSessionsTicketStore.RenewAsync(sessionId, authenticationTicket);

        Assert.Null(await redisSessionsTicketStore.RetrieveAsync(sessionId));
    }

    [Fact]
    public async Task RenewAsyncShouldPreserveTheCreationDateAndTrackedDetails()
    {
        RedisSessionsTicketStore redisSessionsTicketStore = CreateRedisSessionsTicketStore();
        AuthenticationTicket authenticationTicket = CreateAuthenticationTicket("account");
        authenticationTicket.Properties.Items[IdentityConstants.SessionTrackedDetailsProperty] = "{invalid-json";
        string sessionId = await redisSessionsTicketStore.StoreAsync(authenticationTicket);
        AuthenticationTicket renewedTicketAuthenticationTicket = CreateAuthenticationTicket("account");
        renewedTicketAuthenticationTicket.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
        renewedTicketAuthenticationTicket.Properties.Items[IdentityConstants.SessionTrackedDetailsProperty] = "replacement";

        await redisSessionsTicketStore.RenewAsync(sessionId, renewedTicketAuthenticationTicket);

        AuthenticationTicket storedAuthenticationTicket = Assert.IsType<AuthenticationTicket>(await redisSessionsTicketStore.RetrieveAsync(sessionId));
        Assert.Equal(authenticationTicket.Properties.Items[IdentityConstants.SessionCreatedAtProperty], storedAuthenticationTicket.Properties.Items[IdentityConstants.SessionCreatedAtProperty]);
        Assert.Equal("{invalid-json", storedAuthenticationTicket.Properties.Items[IdentityConstants.SessionTrackedDetailsProperty]);
        Assert.Equal(sessionId, storedAuthenticationTicket.Properties.Items[IdentityConstants.SessionIdProperty]);
        Assert.InRange(storedAuthenticationTicket.Properties.ExpiresUtc!.Value - DateTimeOffset.UtcNow, TimeSpan.FromDays(6), TimeSpan.FromDays(7));
        Assert.Null(Assert.Single(await redisSessionsTicketStore.ListAsync("account", cancellationToken: TestContext.Current.CancellationToken)).TrackedDetails);
    }

    [Fact]
    public async Task RenewAsyncShouldIgnoreAMissingSession()
    {
        RedisSessionsTicketStore redisSessionsTicketStore = CreateRedisSessionsTicketStore();

        await redisSessionsTicketStore.RenewAsync("missing", CreateAuthenticationTicket("account"));

        Assert.Null(await redisSessionsTicketStore.RetrieveAsync("missing"));
    }

    [Fact]
    public async Task RenewAsyncShouldThrowInvalidOperationExceptionWhenTheAccountChanges()
    {
        RedisSessionsTicketStore redisSessionsTicketStore = CreateRedisSessionsTicketStore();
        string sessionId = await redisSessionsTicketStore.StoreAsync(CreateAuthenticationTicket("account"));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => redisSessionsTicketStore.RenewAsync(sessionId, CreateAuthenticationTicket("other-account")));

        Assert.Equal("A session cannot change its account.", exception.Message);
        Assert.Equal("account", (await redisSessionsTicketStore.RetrieveAsync(sessionId))!.Principal.FindFirst("sub")!.Value);
    }

    [Fact]
    public async Task RetrieveAsyncShouldReturnNullForAMissingOrUnprotectableTicket()
    {
        RedisSessionsTicketStore redisSessionsTicketStore = CreateRedisSessionsTicketStore();
        await GetConnectionMultiplexer().GetDatabase().StringSetAsync(IdentityConstants.SessionsCachePrefix + "corrupt", new byte[] { 1, 2, 3 });

        AuthenticationTicket? missingAuthenticationTicket = await redisSessionsTicketStore.RetrieveAsync("missing");
        AuthenticationTicket? corruptAuthenticationTicket = await redisSessionsTicketStore.RetrieveAsync("corrupt");

        Assert.Null(missingAuthenticationTicket);
        Assert.Null(corruptAuthenticationTicket);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("missing-created")]
    [InlineData("invalid-created")]
    [InlineData("absolute-expiry")]
    public async Task RetrieveAsyncShouldReturnNullWhenTheProtectedTicketIsInvalidOrExpired(string scenario)
    {
        AuthenticationTicket authenticationTicket = CreateAuthenticationTicket("account");
        authenticationTicket.Properties.Items[IdentityConstants.SessionCreatedAtProperty] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        switch (scenario)
        {
            case "expired":
                authenticationTicket.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
                break;
            case "missing-created":
                authenticationTicket.Properties.Items.Remove(IdentityConstants.SessionCreatedAtProperty);
                break;
            case "invalid-created":
                authenticationTicket.Properties.Items[IdentityConstants.SessionCreatedAtProperty] = "invalid";
                break;
            case "absolute-expiry":
                authenticationTicket.Properties.Items[IdentityConstants.SessionCreatedAtProperty] = DateTimeOffset.UtcNow.AddDays(-8).ToString("O", CultureInfo.InvariantCulture);
                break;
        }
        await SeedTicketAsync("invalid", authenticationTicket);

        AuthenticationTicket? storedAuthenticationTicket = await CreateRedisSessionsTicketStore().RetrieveAsync("invalid");

        Assert.Null(storedAuthenticationTicket);
    }

    [Fact]
    public async Task StoreAsyncShouldReturnTheSessionKeyWhenStoringAnEncryptedTicketWithDefaultExpiryAndAnAccountIndex()
    {
        RedisSessionsTicketStore redisSessionsTicketStore = CreateRedisSessionsTicketStore();
        AuthenticationTicket authenticationTicket = CreateAuthenticationTicket("account");
        authenticationTicket.Properties.ExpiresUtc = null;

        string sessionId = await redisSessionsTicketStore.StoreAsync(authenticationTicket);

        AuthenticationTicket storedAuthenticationTicket = Assert.IsType<AuthenticationTicket>(await redisSessionsTicketStore.RetrieveAsync(sessionId));
        Assert.Equal(64, sessionId.Length);
        Assert.Equal("account", storedAuthenticationTicket.Principal.FindFirst("sub")!.Value);
        Assert.Equal(sessionId, storedAuthenticationTicket.Properties.Items[IdentityConstants.SessionIdProperty]);
        Assert.InRange(storedAuthenticationTicket.Properties.ExpiresUtc!.Value - DateTimeOffset.UtcNow, TimeSpan.FromHours(11), TimeSpan.FromHours(12));
        Assert.True(await GetConnectionMultiplexer().GetDatabase().SetContainsAsync(IdentityConstants.GetAuthSessionsKey("account"), sessionId));
        Assert.Null(TicketSerializer.Default.Deserialize(Assert.IsType<byte[]>((byte[]?)await GetConnectionMultiplexer().GetDatabase().StringGetAsync(IdentityConstants.SessionsCachePrefix + sessionId))));
    }

    [Fact]
    public async Task StoreAsyncShouldReturnTheSessionKeyWhenCappingTheTicketLifetimeAtSevenDays()
    {
        RedisSessionsTicketStore redisSessionsTicketStore = CreateRedisSessionsTicketStore();
        AuthenticationTicket authenticationTicket = CreateAuthenticationTicket("account");
        authenticationTicket.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);

        string sessionId = await redisSessionsTicketStore.StoreAsync(authenticationTicket);

        AuthenticationTicket storedAuthenticationTicket = Assert.IsType<AuthenticationTicket>(await redisSessionsTicketStore.RetrieveAsync(sessionId));
        Assert.InRange(storedAuthenticationTicket.Properties.ExpiresUtc!.Value - DateTimeOffset.UtcNow, TimeSpan.FromDays(6), TimeSpan.FromDays(7));
    }

    [Fact]
    public async Task StoreAsyncShouldThrowInvalidOperationExceptionWhenTheAccountClaimIsMissing()
    {
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateRedisSessionsTicketStore().StoreAsync(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity()), new AuthenticationProperties(), "tests")));

        Assert.Equal("Identity user ID is missing.", exception.Message);
    }

    [Theory]
    [InlineData("store")]
    [InlineData("retrieve")]
    [InlineData("renew")]
    [InlineData("remove")]
    [InlineData("remove_others")]
    [InlineData("list")]
    public async Task StoreAsyncAndRetrieveAsyncAndRenewAsyncAndRemoveAsyncAndRemoveOthersAsyncAndListAsyncShouldThrowRedisConnectionExceptionWhenPropagatingRedisConnectionFailures(string operation)
    {
        await using ConnectionMultiplexer connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(new ConfigurationOptions { EndPoints = { { "127.0.0.1", 1 } }, AbortOnConnectFail = false, ConnectRetry = 0, ConnectTimeout = 250, AsyncTimeout = 250, BacklogPolicy = BacklogPolicy.FailFast });
        RedisSessionsTicketStore redisSessionsTicketStore = new(connectionMultiplexer, GetDataProtectionProvider());

        RedisConnectionException exception = await Assert.ThrowsAsync<RedisConnectionException>(async () =>
        {
            switch (operation)
            {
                case "store":
                    await redisSessionsTicketStore.StoreAsync(CreateAuthenticationTicket("account"));
                    break;
                case "retrieve":
                    await redisSessionsTicketStore.RetrieveAsync("session");
                    break;
                case "renew":
                    await redisSessionsTicketStore.RenewAsync("session", CreateAuthenticationTicket("account"));
                    break;
                case "remove":
                    await redisSessionsTicketStore.RemoveAsync("session");
                    break;
                case "remove_others":
                    await redisSessionsTicketStore.RemoveOthersAsync("account", "session", TestContext.Current.CancellationToken);
                    break;
                case "list":
                    await redisSessionsTicketStore.ListAsync("account", cancellationToken: TestContext.Current.CancellationToken);
                    break;
            }
        });

        Assert.True(AspireBoot.Infrastructure.Observability.DependencyFailures.IsUnavailable(exception));
    }

    private static AuthenticationTicket CreateAuthenticationTicket(string identityUserId) => new(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", identityUserId)], "tests")), new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) }, "tests");

    private RedisSessionsTicketStore CreateRedisSessionsTicketStore() => new(GetConnectionMultiplexer(), GetDataProtectionProvider());

    private IConnectionMultiplexer GetConnectionMultiplexer() => TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IConnectionMultiplexer>();

    private IDataProtectionProvider GetDataProtectionProvider() => TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IDataProtectionProvider>();

    private async Task SeedTicketAsync(string sessionId, AuthenticationTicket authenticationTicket) => await GetConnectionMultiplexer().GetDatabase().StringSetAsync(IdentityConstants.SessionsCachePrefix + sessionId, GetDataProtectionProvider().CreateProtector("AspireBoot.Identity.Sessions.v1").Protect(TicketSerializer.Default.Serialize(authenticationTicket)));
}
