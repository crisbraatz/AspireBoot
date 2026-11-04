using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ConfirmEmail;
using AspireBoot.Core.Integrations.Smtp;
using AspireBoot.Worker.Identity.Auth.ConfirmEmail;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Worker.Identity.Auth.ConfirmEmail;

public class ConfirmEmailEventConsumerTests
{
    [Fact]
    public async Task ShouldDeliverTheReceivedEvent()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        ConfirmEmailEvent confirmEmailEvent = new("account", "user@example.com");

        await new ConfirmEmailEventConsumer(commandHandler).ConsumeAsync(confirmEmailEvent, TestContext.Current.CancellationToken);

        await commandHandler.Received(1).HandleAsync(Arg.Is<SendRequestedEmailCommand>(command => command.EmailRequestedEvent == confirmEmailEvent), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldThrowEmailRequestRateLimitedExceptionWhenClassifyRateLimitingForDelayedDelivery()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        ConfirmEmailEvent confirmEmailEvent = new("account", "user@example.com");
        ConfirmEmailEventConsumer confirmEmailEventConsumer = new(commandHandler);
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.TooManyRequests("email.limited", "Limited.")])));

        Exception? exception = await Record.ExceptionAsync(() => confirmEmailEventConsumer.ConsumeAsync(confirmEmailEvent, TestContext.Current.CancellationToken));

        Assert.IsType<EmailRequestRateLimitedException>(exception);
    }

    [Fact]
    public async Task ShouldThrowInvalidEmailRequestExceptionWhenClassifyInvalidRequestsAsNonRetryable()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        ConfirmEmailEventConsumer confirmEmailEventConsumer = new(commandHandler);
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.Validation("email.invalid", "Invalid.", "email")])));

        InvalidEmailRequestException exception = await Assert.ThrowsAsync<InvalidEmailRequestException>(() => confirmEmailEventConsumer.ConsumeAsync(new ConfirmEmailEvent("account", "user@example.com"), TestContext.Current.CancellationToken));

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ShouldThrowInvalidOperationExceptionWhenPropagatingDependencyFailuresForRetry()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        ConfirmEmailEvent confirmEmailEvent = new("account", "user@example.com");
        ConfirmEmailEventConsumer confirmEmailEventConsumer = new(commandHandler);
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.ServiceUnavailable("smtp.unavailable", "Unavailable.")])));

        Exception? exception = await Record.ExceptionAsync(() => confirmEmailEventConsumer.ConsumeAsync(confirmEmailEvent, TestContext.Current.CancellationToken));

        Assert.IsType<InvalidOperationException>(exception);
    }
}
