namespace AspireBoot.Core.Integrations.Messaging;

public interface IEventRepository
{
    Task AddAsync(IEvent concreteEvent, bool saveChangesAndFlushMessages, CancellationToken cancellationToken);
}
