using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.CleanupAvatars;
using AspireBoot.Worker.Identity.Users.CleanupAvatars;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Worker.Identity.Users.CleanupAvatars;

public class CleanupAvatarsServiceTests
{
    [Fact]
    public async Task ShouldDispatchCleanupWithTheConfiguredMinimumAge()
    {
        DateTimeOffset now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
        TaskCompletionSource<CleanupAvatarsCommand> dispatchedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ICommandHandler<CleanupAvatarsCommand, CleanupAvatarsResult> commandHandler = Substitute.For<ICommandHandler<CleanupAvatarsCommand, CleanupAvatarsResult>>();
        commandHandler.HandleAsync(Arg.Any<CleanupAvatarsCommand>(), Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            dispatchedTaskCompletionSource.TrySetResult(callInfo.Arg<CleanupAvatarsCommand>());
            return Task.FromResult(Result<CleanupAvatarsResult>.Success(new CleanupAvatarsResult(0)));
        });
        await using ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(commandHandler).BuildServiceProvider();
        using CleanupAvatarsService cleanupAvatarsService = CreateCleanupAvatarsService(serviceProvider, MockTimeProvider(now, out Func<Task> tickAsync));
        await cleanupAvatarsService.StartAsync(TestContext.Current.CancellationToken);

        await tickAsync();
        CleanupAvatarsCommand command = await dispatchedTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(now.AddHours(-48), command.OlderThanUtc);
        Assert.Equal(48, command.MinimumAgeInHours);
        await cleanupAvatarsService.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ShouldStopAnActiveCleanupWithoutStartingAnotherRun()
    {
        TaskCompletionSource startedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ICommandHandler<CleanupAvatarsCommand, CleanupAvatarsResult> commandHandler = Substitute.For<ICommandHandler<CleanupAvatarsCommand, CleanupAvatarsResult>>();
        commandHandler.HandleAsync(Arg.Any<CleanupAvatarsCommand>(), Arg.Any<CancellationToken>()).Returns(async callInfo =>
        {
            startedTaskCompletionSource.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, callInfo.Arg<CancellationToken>());
            return Result<CleanupAvatarsResult>.Success(new CleanupAvatarsResult(0));
        });
        await using ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(commandHandler).BuildServiceProvider();
        using CleanupAvatarsService cleanupAvatarsService = CreateCleanupAvatarsService(serviceProvider, MockTimeProvider(DateTimeOffset.UtcNow, out Func<Task> tickAsync));
        await cleanupAvatarsService.StartAsync(TestContext.Current.CancellationToken);
        await tickAsync();
        await startedTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        await cleanupAvatarsService.StopAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(cleanupAvatarsService.ExecuteTask);
        Assert.True(cleanupAvatarsService.ExecuteTask.IsCompletedSuccessfully);
        await commandHandler.Received(1).HandleAsync(Arg.Any<CleanupAvatarsCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldContinueOnTheNextIntervalAfterACleanupFailure()
    {
        TaskCompletionSource firstAttemptTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource recoveredTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int attempts = 0;
        ICommandHandler<CleanupAvatarsCommand, CleanupAvatarsResult> commandHandler = Substitute.For<ICommandHandler<CleanupAvatarsCommand, CleanupAvatarsResult>>();
        commandHandler.HandleAsync(Arg.Any<CleanupAvatarsCommand>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (Interlocked.Increment(ref attempts) is 1)
            {
                firstAttemptTaskCompletionSource.TrySetResult();
                throw new InvalidOperationException("Storage unavailable.");
            }
            recoveredTaskCompletionSource.TrySetResult();
            return Task.FromResult(Result<CleanupAvatarsResult>.Success(new CleanupAvatarsResult(0)));
        });
        await using ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(commandHandler).BuildServiceProvider();
        using CleanupAvatarsService cleanupAvatarsService = CreateCleanupAvatarsService(serviceProvider, MockTimeProvider(DateTimeOffset.UtcNow, out Func<Task> tickAsync));
        await cleanupAvatarsService.StartAsync(TestContext.Current.CancellationToken);

        await tickAsync();
        await firstAttemptTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await tickAsync();
        await recoveredTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(2, attempts);
        await cleanupAvatarsService.StopAsync(TestContext.Current.CancellationToken);
    }

    private static CleanupAvatarsService CreateCleanupAvatarsService(ServiceProvider serviceProvider, TimeProvider timeProvider) => new(new CleanupAvatarsOptions { IntervalInHours = 1, MinimumAgeInHours = 48 }, serviceProvider.GetRequiredService<IServiceScopeFactory>(), timeProvider, NullLogger<CleanupAvatarsService>.Instance);

    private static TimeProvider MockTimeProvider(DateTimeOffset now, out Func<Task> tickAsync)
    {
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(now);
        TaskCompletionSource<Action> timerStartedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        timeProvider.CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>()).Returns(callInfo =>
        {
            TimerCallback timerCallback = callInfo.Arg<TimerCallback>();
            object? state = callInfo.ArgAt<object?>(1);
            timerStartedTaskCompletionSource.TrySetResult(() => timerCallback(state));
            return Substitute.For<ITimer>();
        });
        tickAsync = async () =>
        {
            Action callback = await timerStartedTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            callback();
        };
        return timeProvider;
    }
}
