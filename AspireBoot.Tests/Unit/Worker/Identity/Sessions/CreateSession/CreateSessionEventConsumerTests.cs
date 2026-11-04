using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions;
using AspireBoot.Core.Identity.Sessions.CreateSession;
using AspireBoot.Core.Integrations.Smtp;
using AspireBoot.Worker.Identity.Sessions.CreateSession;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Worker.Identity.Sessions.CreateSession;

public class CreateSessionEventConsumerTests
{
    [Fact]
    public async Task ShouldDeliverTheReceivedEvent()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        CreateSessionEvent createSessionEvent = new("account", "user@example.com", DateTimeOffset.UtcNow, new TrackedDetails("Chrome", "Linux", "Desktop", "127.0.0.1"));

        await new CreateSessionEventConsumer(commandHandler).ConsumeAsync(createSessionEvent, TestContext.Current.CancellationToken);

        await commandHandler.Received(1).HandleAsync(Arg.Is<SendRequestedEmailCommand>(command => command.EmailRequestedEvent == createSessionEvent), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldThrowEmailRequestRateLimitedExceptionWhenClassifyRateLimitingForDelayedDelivery()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        CreateSessionEvent createSessionEvent = new("account", "user@example.com", DateTimeOffset.UtcNow, new TrackedDetails("Chrome", "Linux", "Desktop", "127.0.0.1"));
        CreateSessionEventConsumer createSessionEventConsumer = new(commandHandler);
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.TooManyRequests("email.limited", "Limited.")])));

        Exception? exception = await Record.ExceptionAsync(() => createSessionEventConsumer.ConsumeAsync(createSessionEvent, TestContext.Current.CancellationToken));

        Assert.IsType<EmailRequestRateLimitedException>(exception);
    }

    [Fact]
    public async Task ShouldThrowInvalidEmailRequestExceptionWhenClassifyInvalidRequestsAsNonRetryable()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        CreateSessionEventConsumer createSessionEventConsumer = new(commandHandler);
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.Validation("email.invalid", "Invalid.", "email")])));

        InvalidEmailRequestException exception = await Assert.ThrowsAsync<InvalidEmailRequestException>(() => createSessionEventConsumer.ConsumeAsync(new CreateSessionEvent("account", "user@example.com", DateTimeOffset.UtcNow, new TrackedDetails("Chrome", "Linux", "Desktop", "127.0.0.1")), TestContext.Current.CancellationToken));

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ShouldThrowInvalidOperationExceptionWhenPropagatingDependencyFailuresForRetry()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        CreateSessionEvent createSessionEvent = new("account", "user@example.com", DateTimeOffset.UtcNow, new TrackedDetails("Chrome", "Linux", "Desktop", "127.0.0.1"));
        CreateSessionEventConsumer createSessionEventConsumer = new(commandHandler);
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.ServiceUnavailable("smtp.unavailable", "Unavailable.")])));

        Exception? exception = await Record.ExceptionAsync(() => createSessionEventConsumer.ConsumeAsync(createSessionEvent, TestContext.Current.CancellationToken));

        Assert.IsType<InvalidOperationException>(exception);
    }
}
