using AspireBoot.Core.Identity.Auth.SendEmailConfirmation;
using AspireBoot.Infrastructure.Integrations.Rabbit;
using Wolverine;

namespace AspireBoot.Tests.Unit.Infrastructure.Integrations.Rabbit;

public class RabbitEventEnvelopeRuleTests
{
    [Theory]
    [InlineData("correlation-id")]
    [InlineData(null)]
    public void ShouldCopyTheEventIdentityAndCorrelationIntoTheEnvelope(string? correlationId = null)
    {
        SendEmailConfirmationEvent sendEmailConfirmationEvent = new("account", "user@example.com", "https://app.tests") { CorrelationId = correlationId };
        Envelope envelope = new(sendEmailConfirmationEvent) { Id = Guid.NewGuid(), CorrelationId = "old-correlation" };

        new RabbitEventEnvelopeRule().Modify(envelope);

        Assert.Equal(sendEmailConfirmationEvent.MessageId, envelope.Id);
        Assert.Equal(correlationId, envelope.CorrelationId);
        Assert.Same(sendEmailConfirmationEvent, envelope.Message);
    }

    [Theory]
    [InlineData("not-an-event")]
    [InlineData(null)]
    public void ShouldPreserveAnEnvelopeThatDoesNotContainAnEvent(string? message = null)
    {
        Guid messageId = Guid.NewGuid();
        Envelope envelope = new() { Message = message, Id = messageId, CorrelationId = "original-correlation" };

        new RabbitEventEnvelopeRule().Modify(envelope);

        Assert.Equal(messageId, envelope.Id);
        Assert.Equal("original-correlation", envelope.CorrelationId);
    }
}
