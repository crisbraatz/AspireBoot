using System.Security.Cryptography;
using System.Text;
using AspireBoot.Core;
using AspireBoot.Core.Cqrs;
using AspireBoot.Infrastructure.Integrations.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AspireBoot.Tests.Integration.Infrastructure.Integrations.Redis;

[Trait("Category", "Integration")]
public class RedisEmailRequestLimiterTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldReturnSuccessWhenAllowingTheFirstRequestAndExpireBothQuotaKeys()
    {
        IConnectionMultiplexer connectionMultiplexer = GetConnectionMultiplexer();

        Result<bool> result = await CreateRedisEmailRequestLimiter(connectionMultiplexer).CanSendAsync("account", "user@example.com", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        foreach (RedisKey redisKey in GetQuotaKeys("account", "user@example.com"))
        {
            Assert.Equal(1, await connectionMultiplexer.GetDatabase().SortedSetLengthAsync(redisKey));
            TimeSpan timeToLive = Assert.IsType<TimeSpan>(await connectionMultiplexer.GetDatabase().KeyTimeToLiveAsync(redisKey));
            Assert.InRange(timeToLive, TimeSpan.FromSeconds(50), TimeSpan.FromMinutes(1));
        }
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenAllowingNewRequestsAfterTheWindowHasElapsed()
    {
        IConnectionMultiplexer connectionMultiplexer = GetConnectionMultiplexer();
        RedisEmailRequestLimiter redisEmailRequestLimiter = CreateRedisEmailRequestLimiter(connectionMultiplexer);
        foreach (RedisKey redisKey in GetQuotaKeys("account", "user@example.com"))
            await connectionMultiplexer.GetDatabase().SortedSetAddAsync(redisKey, "expired", DateTimeOffset.UtcNow.AddMinutes(-2).ToUnixTimeMilliseconds());

        Result<bool> result = await redisEmailRequestLimiter.CanSendAsync("account", "user@example.com", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Value);
        foreach (RedisKey keyRedisKey in GetQuotaKeys("account", "user@example.com"))
        {
            Assert.Null(await connectionMultiplexer.GetDatabase().SortedSetScoreAsync(keyRedisKey, "expired"));
            Assert.Equal(1, await connectionMultiplexer.GetDatabase().SortedSetLengthAsync(keyRedisKey));
        }
    }

    [Theory]
    [InlineData("account", "other@example.com")]
    [InlineData("other-account", "USER@EXAMPLE.COM")]
    public async Task ShouldReturnSuccessWhenApplyingCooldownToBothTheAccountAndNormalizedAddress(string identityUserId, string email)
    {
        RedisEmailRequestLimiter redisEmailRequestLimiter = CreateRedisEmailRequestLimiter(GetConnectionMultiplexer(), 10);
        await redisEmailRequestLimiter.CanSendAsync("account", "user@example.com", cancellationToken: TestContext.Current.CancellationToken);

        Result<bool> result = await redisEmailRequestLimiter.CanSendAsync(identityUserId, email, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenAllowingTheSameRequestIdAgainWithoutConsumingAnotherPermit()
    {
        IConnectionMultiplexer connectionMultiplexer = GetConnectionMultiplexer();
        RedisEmailRequestLimiter redisEmailRequestLimiter = CreateRedisEmailRequestLimiter(connectionMultiplexer);
        Guid requestId = Guid.NewGuid();
        await redisEmailRequestLimiter.CanSendAsync("account", "user@example.com", requestId, TestContext.Current.CancellationToken);

        Result<bool> result = await redisEmailRequestLimiter.CanSendAsync("account", "user@example.com", requestId, TestContext.Current.CancellationToken);

        Assert.True(result.Value);
        foreach (RedisKey redisKey in GetQuotaKeys("account", "user@example.com"))
            Assert.Equal(1, await connectionMultiplexer.GetDatabase().SortedSetLengthAsync(redisKey));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task ShouldReturnSuccessWhenAnExhaustedAccountOrAddressQuotaAfterCooldownIsProvided(int quotaIndex)
    {
        IConnectionMultiplexer connectionMultiplexer = GetConnectionMultiplexer();
        RedisEmailRequestLimiter redisEmailRequestLimiter = CreateRedisEmailRequestLimiter(connectionMultiplexer);
        RedisKey quotaKeyRedisKey = GetQuotaKeys("account", "user@example.com")[quotaIndex];
        await connectionMultiplexer.GetDatabase().SortedSetAddAsync(quotaKeyRedisKey, "previous", DateTimeOffset.UtcNow.AddSeconds(-5).ToUnixTimeMilliseconds());

        Result<bool> result = await redisEmailRequestLimiter.CanSendAsync("account", "user@example.com", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
        Assert.Equal(1, await connectionMultiplexer.GetDatabase().SortedSetLengthAsync(quotaKeyRedisKey));
        Assert.Equal(0, await connectionMultiplexer.GetDatabase().SortedSetLengthAsync(GetQuotaKeys("account", "user@example.com")[1 - quotaIndex]));
    }

    [Fact]
    public async Task ShouldThrowRedisConnectionExceptionWhenPreservingRedisFailuresInsteadOfAllowingDelivery()
    {
        await using ConnectionMultiplexer connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(new ConfigurationOptions { EndPoints = { { "127.0.0.1", 1 } }, AbortOnConnectFail = false, ConnectRetry = 0, ConnectTimeout = 250, AsyncTimeout = 250, BacklogPolicy = BacklogPolicy.FailFast });

        RedisConnectionException exception = await Assert.ThrowsAsync<RedisConnectionException>(() => CreateRedisEmailRequestLimiter(connectionMultiplexer).CanSendAsync("account", "user@example.com", cancellationToken: TestContext.Current.CancellationToken));

        Assert.True(AspireBoot.Infrastructure.Observability.DependencyFailures.IsUnavailable(exception));
    }

    private static RedisEmailRequestLimiter CreateRedisEmailRequestLimiter(IConnectionMultiplexer connectionMultiplexer, int permitLimit = 1) => new(connectionMultiplexer, new EmailRequestsOptions { CooldownInSeconds = 1, PermitLimit = permitLimit, WindowInMinutes = 1 });

    private IConnectionMultiplexer GetConnectionMultiplexer() => TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IConnectionMultiplexer>();

    private static RedisKey[] GetQuotaKeys(string identityUserId, string email) => ["identity:email:{requests}:account:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identityUserId))), "identity:email:{requests}:address:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.ToUpperInvariant())))];
}
