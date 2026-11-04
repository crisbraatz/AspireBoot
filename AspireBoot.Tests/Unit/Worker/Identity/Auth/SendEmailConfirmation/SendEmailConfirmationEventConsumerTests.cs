using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.SendEmailConfirmation;
using AspireBoot.Core.Integrations.Smtp;
using AspireBoot.Worker.Identity.Auth.SendEmailConfirmation;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Worker.Identity.Auth.SendEmailConfirmation;

public class SendEmailConfirmationEventConsumerTests
{
    [Fact]
    public async Task ShouldDeliverTheReceivedEvent()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        SendEmailConfirmationEvent sendEmailConfirmationEvent = new("account", "user@example.com", "https://app.tests");

        await new SendEmailConfirmationEventConsumer(commandHandler).ConsumeAsync(sendEmailConfirmationEvent, TestContext.Current.CancellationToken);

        await commandHandler.Received(1).HandleAsync(Arg.Is<SendRequestedEmailCommand>(command => command.EmailRequestedEvent == sendEmailConfirmationEvent), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldThrowEmailRequestRateLimitedExceptionWhenClassifyRateLimitingForDelayedDelivery()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        SendEmailConfirmationEvent sendEmailConfirmationEvent = new("account", "user@example.com", "https://app.tests");
        SendEmailConfirmationEventConsumer sendEmailConfirmationEventConsumer = new(commandHandler);
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.TooManyRequests("email.limited", "Limited.")])));

        Exception? exception = await Record.ExceptionAsync(() => sendEmailConfirmationEventConsumer.ConsumeAsync(sendEmailConfirmationEvent, TestContext.Current.CancellationToken));

        Assert.IsType<EmailRequestRateLimitedException>(exception);
    }

    [Fact]
    public async Task ShouldThrowInvalidEmailRequestExceptionWhenClassifyInvalidRequestsAsNonRetryable()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        SendEmailConfirmationEventConsumer sendEmailConfirmationEventConsumer = new(commandHandler);
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.Validation("email.invalid", "Invalid.", "email")])));

        InvalidEmailRequestException exception = await Assert.ThrowsAsync<InvalidEmailRequestException>(() => sendEmailConfirmationEventConsumer.ConsumeAsync(new SendEmailConfirmationEvent("account", "user@example.com", "https://app.tests"), TestContext.Current.CancellationToken));

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ShouldThrowInvalidOperationExceptionWhenPropagatingDependencyFailuresForRetry()
    {
        ICommandHandler<SendRequestedEmailCommand> commandHandler = Substitute.For<ICommandHandler<SendRequestedEmailCommand>>();
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        SendEmailConfirmationEvent sendEmailConfirmationEvent = new("account", "user@example.com", "https://app.tests");
        SendEmailConfirmationEventConsumer sendEmailConfirmationEventConsumer = new(commandHandler);
        commandHandler.HandleAsync(Arg.Any<SendRequestedEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.ServiceUnavailable("smtp.unavailable", "Unavailable.")])));

        Exception? exception = await Record.ExceptionAsync(() => sendEmailConfirmationEventConsumer.ConsumeAsync(sendEmailConfirmationEvent, TestContext.Current.CancellationToken));

        Assert.IsType<InvalidOperationException>(exception);
    }
}
