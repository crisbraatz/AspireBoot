using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.PasswordChanged;
using AspireBoot.Core.Integrations.Smtp;
using AspireBoot.Worker.Identity.Auth.PasswordChanged;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Worker.Identity.Auth.PasswordChanged;

public class PasswordChangedEventConsumerTests
{
    [Fact]
    public async Task ShouldDeliverTheReceivedEvent()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        PasswordChangedEvent passwordChangedEvent = new("account", "user@example.com");

        await new PasswordChangedEventConsumer(commandHandler).ConsumeAsync(passwordChangedEvent, TestContext.Current.CancellationToken);

        await commandHandler.Received(1).HandleAsync(Arg.Is<SendRequestedEmailCommand>(command => command.EmailRequestedEvent == passwordChangedEvent), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldThrowEmailRequestRateLimitedExceptionWhenClassifyRateLimitingForDelayedDelivery()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        PasswordChangedEvent passwordChangedEvent = new("account", "user@example.com");
        PasswordChangedEventConsumer passwordChangedEventConsumer = new(commandHandler);
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.TooManyRequests("email.limited", "Limited.")])));

        Exception? exception = await Record.ExceptionAsync(() => passwordChangedEventConsumer.ConsumeAsync(passwordChangedEvent, TestContext.Current.CancellationToken));

        Assert.IsType<EmailRequestRateLimitedException>(exception);
    }

    [Fact]
    public async Task ShouldThrowInvalidEmailRequestExceptionWhenClassifyInvalidRequestsAsNonRetryable()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        PasswordChangedEventConsumer passwordChangedEventConsumer = new(commandHandler);
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.Validation("email.invalid", "Invalid.", "email")])));

        InvalidEmailRequestException exception = await Assert.ThrowsAsync<InvalidEmailRequestException>(() => passwordChangedEventConsumer.ConsumeAsync(new PasswordChangedEvent("account", "user@example.com"), TestContext.Current.CancellationToken));

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ShouldThrowInvalidOperationExceptionWhenPropagatingDependencyFailuresForRetry()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        PasswordChangedEvent passwordChangedEvent = new("account", "user@example.com");
        PasswordChangedEventConsumer passwordChangedEventConsumer = new(commandHandler);
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.ServiceUnavailable("smtp.unavailable", "Unavailable.")])));

        Exception? exception = await Record.ExceptionAsync(() => passwordChangedEventConsumer.ConsumeAsync(passwordChangedEvent, TestContext.Current.CancellationToken));

        Assert.IsType<InvalidOperationException>(exception);
    }
}
