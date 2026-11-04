using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.RequestRecovery;
using AspireBoot.Core.Integrations.Smtp;
using AspireBoot.Worker.Identity.Auth.RequestRecovery;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Worker.Identity.Auth.RequestRecovery;

public class RequestRecoveryEventConsumerTests
{
    [Fact]
    public async Task ShouldDeliverTheReceivedEvent()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        RequestRecoveryEvent requestRecoveryEvent = new("account", "user@example.com", "https://app.tests");

        await new RequestRecoveryEventConsumer(commandHandler).ConsumeAsync(requestRecoveryEvent, TestContext.Current.CancellationToken);

        await commandHandler.Received(1).HandleAsync(Arg.Is<SendRequestedEmailCommand>(command => command.EmailRequestedEvent == requestRecoveryEvent), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldThrowEmailRequestRateLimitedExceptionWhenClassifyRateLimitingForDelayedDelivery()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        RequestRecoveryEvent requestRecoveryEvent = new("account", "user@example.com", "https://app.tests");
        RequestRecoveryEventConsumer requestRecoveryEventConsumer = new(commandHandler);
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.TooManyRequests("email.limited", "Limited.")])));

        Exception? exception = await Record.ExceptionAsync(() => requestRecoveryEventConsumer.ConsumeAsync(requestRecoveryEvent, TestContext.Current.CancellationToken));

        Assert.IsType<EmailRequestRateLimitedException>(exception);
    }

    [Fact]
    public async Task ShouldThrowInvalidEmailRequestExceptionWhenClassifyInvalidRequestsAsNonRetryable()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        RequestRecoveryEventConsumer requestRecoveryEventConsumer = new(commandHandler);
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.Validation("email.invalid", "Invalid.", "email")])));

        InvalidEmailRequestException exception = await Assert.ThrowsAsync<InvalidEmailRequestException>(() => requestRecoveryEventConsumer.ConsumeAsync(new RequestRecoveryEvent("account", "user@example.com", "https://app.tests"), TestContext.Current.CancellationToken));

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ShouldThrowInvalidOperationExceptionWhenPropagatingDependencyFailuresForRetry()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        RequestRecoveryEvent requestRecoveryEvent = new("account", "user@example.com", "https://app.tests");
        RequestRecoveryEventConsumer requestRecoveryEventConsumer = new(commandHandler);
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.ServiceUnavailable("smtp.unavailable", "Unavailable.")])));

        Exception? exception = await Record.ExceptionAsync(() => requestRecoveryEventConsumer.ConsumeAsync(requestRecoveryEvent, TestContext.Current.CancellationToken));

        Assert.IsType<InvalidOperationException>(exception);
    }
}
