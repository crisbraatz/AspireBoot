using AspireBoot.Core.Cqrs;
using AspireBoot.Infrastructure.Integrations.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace AspireBoot.Tests.Integration.Infrastructure.Integrations.Redis;

[Trait("Category", "Integration")]
public class RedisCachingTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task AddToSetAsyncShouldReturnSuccessWhenMaintainingUniqueSetMembersAndRefreshingTheExpiration()
    {
        IConnectionMultiplexer connectionMultiplexer = GetConnectionMultiplexer();
        RedisCaching redisCaching = CreateRedisCaching(connectionMultiplexer);
        string key = Guid.NewGuid().ToString();
        await redisCaching.AddToSetAsync(key, "first", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
        await redisCaching.AddToSetAsync(key, "second", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);

        Result result = await redisCaching.AddToSetAsync(key, "first", TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Result<IReadOnlySet<string>> membersResult = await redisCaching.GetSetAsync(key, TestContext.Current.CancellationToken);
        Assert.True(membersResult.IsSuccess);
        Assert.Equal(2, membersResult.Value!.Count);
        Assert.Contains("first", membersResult.Value);
        Assert.Contains("second", membersResult.Value);
        Assert.InRange(Assert.IsType<TimeSpan>(await connectionMultiplexer.GetDatabase().KeyTimeToLiveAsync(key)), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));
    }

    [Theory]
    [InlineData("add_to_set")]
    [InlineData("get")]
    [InlineData("get_and_remove")]
    [InlineData("get_set")]
    [InlineData("remove_from_set")]
    public async Task ExecuteAsyncShouldThrowRedisServerExceptionWhenTheRedisValueHasTheWrongType(string operation)
    {
        IConnectionMultiplexer connectionMultiplexer = GetConnectionMultiplexer();
        RedisCaching redisCaching = CreateRedisCaching(connectionMultiplexer);
        string key = Guid.NewGuid().ToString();
        if (operation is "get" or "get_and_remove")
            await connectionMultiplexer.GetDatabase().SetAddAsync(key, "member");
        else
            await connectionMultiplexer.GetDatabase().StringSetAsync(key, "string");

        RedisServerException exception = await Assert.ThrowsAsync<RedisServerException>(() => ExecuteAsync(redisCaching, operation, key, TestContext.Current.CancellationToken));

        Assert.Equal(RedisErrorKind.WrongType, exception.Kind);
        Assert.True(await connectionMultiplexer.GetDatabase().KeyExistsAsync(key));
    }

    [Theory]
    [InlineData("add_to_set")]
    [InlineData("get")]
    [InlineData("get_and_remove")]
    [InlineData("get_set")]
    [InlineData("remove")]
    [InlineData("remove_from_set")]
    [InlineData("set")]
    public async Task ExecuteAsyncShouldThrowOperationCanceledExceptionWhenCancellationIsRequestedWhileRedisCommandsArePending(string operation)
    {
        IConnectionMultiplexer connectionMultiplexer = GetConnectionMultiplexer();
        await using ConnectionMultiplexer controlConnectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(connectionMultiplexer.Configuration);
        RedisCaching redisCaching = CreateRedisCaching(connectionMultiplexer);
        using CancellationTokenSource cancellationTokenSource = new();
        string key = Guid.NewGuid().ToString();
        await controlConnectionMultiplexer.GetDatabase().ExecuteAsync("CLIENT", "PAUSE", 1000, "ALL");
        try
        {
            Task<Result> pendingCommand = ExecuteAsync(redisCaching, operation, key, cancellationTokenSource.Token);
            Assert.False(pendingCommand.IsCompleted);
            await cancellationTokenSource.CancelAsync();

            OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pendingCommand);

            Assert.Equal(cancellationTokenSource.Token, exception.CancellationToken);
        }
        finally
        {
            await controlConnectionMultiplexer.GetDatabase().ExecuteAsync("CLIENT", "UNPAUSE");
            await connectionMultiplexer.GetDatabase().PingAsync();
        }
    }

    [Theory]
    [InlineData("set")]
    [InlineData("remove")]
    public async Task ExecuteAsyncShouldThrowRedisServerExceptionWhenRedisRejectsTheOperation(string operation)
    {
        IConnectionMultiplexer connectionMultiplexer = GetConnectionMultiplexer();
        string username = "tests-" + Guid.NewGuid().ToString("N");
        string password = Guid.NewGuid().ToString("N");
        await connectionMultiplexer.GetDatabase().ExecuteAsync("ACL", "SETUSER", username, "on", ">" + password, "~*", "+@all", "-@write");
        try
        {
            ConfigurationOptions configurationOptions = ConfigurationOptions.Parse(connectionMultiplexer.Configuration);
            configurationOptions.User = username;
            configurationOptions.Password = password;
            await using ConnectionMultiplexer restrictedConnectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(configurationOptions);
            Assert.Equal(username, (string?)await restrictedConnectionMultiplexer.GetDatabase().ExecuteAsync("ACL", "WHOAMI"));
            RedisCaching redisCaching = CreateRedisCaching(restrictedConnectionMultiplexer);

            RedisServerException exception = await Assert.ThrowsAsync<RedisServerException>(() => ExecuteAsync(redisCaching, operation, Guid.NewGuid().ToString(), TestContext.Current.CancellationToken));

            Assert.Equal(RedisErrorKind.NoPermission, exception.Kind);
        }
        finally
        {
            await connectionMultiplexer.GetDatabase().ExecuteAsync("ACL", "DELUSER", username);
        }
    }

    [Theory]
    [InlineData("add_to_set")]
    [InlineData("get")]
    [InlineData("get_and_remove")]
    [InlineData("get_set")]
    [InlineData("remove")]
    [InlineData("remove_from_set")]
    [InlineData("set")]
    public async Task ExecuteAsyncShouldReturnAServiceUnavailableFailureWhenRedisCannotBeReached(string operation)
    {
        await using ConnectionMultiplexer connectionMultiplexer = await CreateUnavailableConnectionAsync();

        Result result = await ExecuteAsync(CreateRedisCaching(connectionMultiplexer), operation, Guid.NewGuid().ToString(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Failure failure = Assert.Single(result.Failures!);
        Assert.Equal(FailureKind.ServiceUnavailable, failure.FailureKind);
        Assert.Equal("caching.unavailable", failure.Code);
    }

    [Fact]
    public async Task GetAndRemoveAsyncShouldReturnSuccessWhenReadingAndRemovingAStringAtomically()
    {
        RedisCaching redisCaching = CreateRedisCaching(GetConnectionMultiplexer());
        string key = Guid.NewGuid().ToString();
        await redisCaching.SetAsync(key, "value", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);

        Result<string?> result = await redisCaching.GetAndRemoveAsync(key, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
        Assert.Null((await redisCaching.GetAsync(key, TestContext.Current.CancellationToken)).Value);
    }

    [Fact]
    public async Task GetAsyncAndGetAndRemoveAsyncAndGetSetAsyncShouldReturnSuccessWhenPreservingEmptyStringsInValuesAndSetMembers()
    {
        RedisCaching redisCaching = CreateRedisCaching(GetConnectionMultiplexer());
        string key = Guid.NewGuid().ToString();
        string setKey = Guid.NewGuid().ToString();
        Assert.True((await redisCaching.SetAsync(key, string.Empty, TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken)).IsSuccess);
        Assert.True((await redisCaching.AddToSetAsync(setKey, string.Empty, TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken)).IsSuccess);

        Result<string?> getResult = await redisCaching.GetAsync(key, TestContext.Current.CancellationToken);
        Result<string?> getAndRemoveResult = await redisCaching.GetAndRemoveAsync(key, TestContext.Current.CancellationToken);
        Result<IReadOnlySet<string>> getSetResult = await redisCaching.GetSetAsync(setKey, TestContext.Current.CancellationToken);

        Assert.True(getResult.IsSuccess);
        Assert.Equal(string.Empty, getResult.Value);
        Assert.True(getAndRemoveResult.IsSuccess);
        Assert.Equal(string.Empty, getAndRemoveResult.Value);
        Assert.True(getSetResult.IsSuccess);
        Assert.Equal(string.Empty, Assert.Single(getSetResult.Value!));
        Assert.Null((await redisCaching.GetAsync(key, TestContext.Current.CancellationToken)).Value);
    }

    [Fact]
    public async Task GetAsyncAndGetAndRemoveAsyncAndGetSetAsyncShouldReturnSuccessfulMissesForMissingStringsAndSets()
    {
        RedisCaching redisCaching = CreateRedisCaching(GetConnectionMultiplexer());
        string key = Guid.NewGuid().ToString();

        Result<string?> getResult = await redisCaching.GetAsync(key, TestContext.Current.CancellationToken);
        Result<string?> getAndRemoveResult = await redisCaching.GetAndRemoveAsync(key, TestContext.Current.CancellationToken);
        Result<IReadOnlySet<string>> getSetResult = await redisCaching.GetSetAsync(key, TestContext.Current.CancellationToken);

        Assert.True(getResult.IsSuccess);
        Assert.Null(getResult.Value);
        Assert.True(getAndRemoveResult.IsSuccess);
        Assert.Null(getAndRemoveResult.Value);
        Assert.True(getSetResult.IsSuccess);
        Assert.Empty(getSetResult.Value!);
    }

    [Fact]
    public async Task RemoveAsyncShouldReturnSuccessWhenRemovingAnExistingKeyAndAllowingRemovingItAgain()
    {
        RedisCaching redisCaching = CreateRedisCaching(GetConnectionMultiplexer());
        string key = Guid.NewGuid().ToString();
        await redisCaching.SetAsync(key, "value", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);

        Result result = await redisCaching.RemoveAsync(key, TestContext.Current.CancellationToken);
        Result repeatedResult = await redisCaching.RemoveAsync(key, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(repeatedResult.IsSuccess);
        Assert.Null((await redisCaching.GetAsync(key, TestContext.Current.CancellationToken)).Value);
    }

    [Fact]
    public async Task RemoveFromSetAsyncShouldReturnSuccessWhenRemovingOnlyTheRequestedSetMember()
    {
        RedisCaching redisCaching = CreateRedisCaching(GetConnectionMultiplexer());
        string key = Guid.NewGuid().ToString();
        await redisCaching.AddToSetAsync(key, "first", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
        await redisCaching.AddToSetAsync(key, "second", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);

        Result result = await redisCaching.RemoveFromSetAsync(key, "first", TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("second", Assert.Single((await redisCaching.GetSetAsync(key, TestContext.Current.CancellationToken)).Value!));
        Assert.True((await redisCaching.RemoveFromSetAsync(key, "missing", TestContext.Current.CancellationToken)).IsSuccess);
    }

    [Fact]
    public async Task SetAsyncShouldReturnSuccessWhenStoringAndReplacingAStringWithAnExpiration()
    {
        IConnectionMultiplexer connectionMultiplexer = GetConnectionMultiplexer();
        RedisCaching redisCaching = CreateRedisCaching(connectionMultiplexer);
        string key = Guid.NewGuid().ToString();
        await redisCaching.SetAsync(key, "old", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);

        Result result = await redisCaching.SetAsync(key, "new", TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("new", (await redisCaching.GetAsync(key, TestContext.Current.CancellationToken)).Value);
        Assert.InRange(Assert.IsType<TimeSpan>(await connectionMultiplexer.GetDatabase().KeyTimeToLiveAsync(key)), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));
    }

    [Fact]
    public async Task SetAsyncShouldReturnServiceUnavailableWhileReadsRemainAvailable()
    {
        IConnectionMultiplexer connectionMultiplexer = GetConnectionMultiplexer();
        IServer server = connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints().Single());
        string originalMinimumReplicas = Assert.Single(await server.ConfigGetAsync("min-replicas-to-write")).Value;
        RedisCaching redisCaching = CreateRedisCaching(connectionMultiplexer);
        string key = Guid.NewGuid().ToString();
        Assert.True((await redisCaching.SetAsync(key, "original", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken)).IsSuccess);
        await server.ConfigSetAsync("min-replicas-to-write", 1);

        try
        {
            Result result = await redisCaching.SetAsync(key, "replacement", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccess);
            Failure failure = Assert.Single(result.Failures!);
            Assert.Equal(FailureKind.ServiceUnavailable, failure.FailureKind);
            Assert.Equal("caching.unavailable", failure.Code);
            Result<string?> readResult = await redisCaching.GetAsync(key, TestContext.Current.CancellationToken);
            Assert.True(readResult.IsSuccess);
            Assert.Equal("original", readResult.Value);
        }
        finally
        {
            await server.ConfigSetAsync("min-replicas-to-write", originalMinimumReplicas);
        }
    }

    private static RedisCaching CreateRedisCaching(IConnectionMultiplexer connectionMultiplexer) => new(connectionMultiplexer, NullLogger<RedisCaching>.Instance);

    private static Task<ConnectionMultiplexer> CreateUnavailableConnectionAsync() => ConnectionMultiplexer.ConnectAsync(new ConfigurationOptions { EndPoints = { { "127.0.0.1", 1 } }, AbortOnConnectFail = false, ConnectRetry = 0, ConnectTimeout = 250, AsyncTimeout = 250, BacklogPolicy = BacklogPolicy.FailFast });

    private static async Task<Result> ExecuteAsync(RedisCaching redisCaching, string operation, string key, CancellationToken cancellationToken) => operation switch { "add_to_set" => await redisCaching.AddToSetAsync(key, "value", TimeSpan.FromMinutes(1), cancellationToken), "get" => await redisCaching.GetAsync(key, cancellationToken), "get_and_remove" => await redisCaching.GetAndRemoveAsync(key, cancellationToken), "get_set" => await redisCaching.GetSetAsync(key, cancellationToken), "remove" => await redisCaching.RemoveAsync(key, cancellationToken), "remove_from_set" => await redisCaching.RemoveFromSetAsync(key, "value", cancellationToken), "set" => await redisCaching.SetAsync(key, "value", TimeSpan.FromMinutes(1), cancellationToken), _ => throw new ArgumentOutOfRangeException(nameof(operation)) };

    private IConnectionMultiplexer GetConnectionMultiplexer() => TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IConnectionMultiplexer>();
}
