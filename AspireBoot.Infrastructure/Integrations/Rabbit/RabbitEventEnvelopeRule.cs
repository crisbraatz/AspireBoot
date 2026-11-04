using AspireBoot.Core.Integrations.Messaging;
using Wolverine;

namespace AspireBoot.Infrastructure.Integrations.Rabbit;

public class RabbitEventEnvelopeRule : IEnvelopeRule
{
    public void Modify(Envelope envelope)
    {
        if (envelope.Message is not IEvent concreteEvent)
            return;

        envelope.Id = concreteEvent.MessageId;
        envelope.CorrelationId = concreteEvent.CorrelationId;
    }
}
