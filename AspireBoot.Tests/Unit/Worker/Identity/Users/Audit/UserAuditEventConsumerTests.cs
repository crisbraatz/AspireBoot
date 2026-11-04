using AspireBoot.Core.Identity.Users.Audit;
using AspireBoot.Worker.Identity.Users.Audit;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Worker.Identity.Users.Audit;

public class UserAuditEventConsumerTests
{
    [Fact]
    public async Task ShouldPersistTheReceivedAudit()
    {
        IUserAuditRepository userAuditRepository = Substitute.For<IUserAuditRepository>();
        UserAuditEvent userAuditEvent = new(Guid.NewGuid(), UserAuditEvent.Update, [UserAuditFields.Name]);

        await new UserAuditEventConsumer(userAuditRepository).ConsumeAsync(userAuditEvent, TestContext.Current.CancellationToken);

        await userAuditRepository.Received(1).AddAsync(userAuditEvent, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ShouldPropagatePersistenceFailuresForRetry()
    {
        IUserAuditRepository userAuditRepository = Substitute.For<IUserAuditRepository>();
        UserAuditEvent userAuditEvent = new(Guid.NewGuid(), UserAuditEvent.Update, [UserAuditFields.Name]);
        InvalidOperationException invalidOperationException = new("Audit database unavailable.");
        userAuditRepository.AddAsync(userAuditEvent, Arg.Any<CancellationToken>()).Returns(Task.FromException(invalidOperationException));
        UserAuditEventConsumer userAuditEventConsumer = new(userAuditRepository);

        Exception? exception = await Record.ExceptionAsync(() => userAuditEventConsumer.ConsumeAsync(userAuditEvent, TestContext.Current.CancellationToken));

        Assert.Same(invalidOperationException, exception);
    }
}
