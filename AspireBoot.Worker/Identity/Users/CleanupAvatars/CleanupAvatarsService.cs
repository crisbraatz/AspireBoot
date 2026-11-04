using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.CleanupAvatars;
using AspireBoot.Infrastructure.Observability;

namespace AspireBoot.Worker.Identity.Users.CleanupAvatars;

public class CleanupAvatarsService(
    CleanupAvatarsOptions cleanupAvatarsOptions,
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider,
    ILogger<CleanupAvatarsService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer periodicTimer =
            new(TimeSpan.FromHours(cleanupAvatarsOptions.IntervalInHours), timeProvider);

        try
        {
            while (await periodicTimer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    AsyncServiceScope asyncServiceScope = serviceScopeFactory.CreateAsyncScope();

                    await using (asyncServiceScope.ConfigureAwait(false))
                        await asyncServiceScope.ServiceProvider
                            .GetRequiredService<ICommandHandler<CleanupAvatarsCommand, CleanupAvatarsResult>>()
                            .HandleAsync(
                                new CleanupAvatarsCommand(
                                    timeProvider.GetUtcNow().AddHours(-cleanupAvatarsOptions.MinimumAgeInHours),
                                    cleanupAvatarsOptions.MinimumAgeInHours),
                                stoppingToken)
                            .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
#pragma warning disable CA1031
                catch (Exception exception)
#pragma warning restore CA1031
                {
                    DependencyLogs.LogDependencyFailure(
                        logger,
                        DependencyKind.ObjectStorage,
                        "execute",
                        exception.GetType().Name,
                        "RustFS integration failed.",
                        exception);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // IsCancellationRequested
        }
    }
}
