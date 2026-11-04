using AspireBoot.Core.Integrations.Messaging;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Wolverine.EntityFrameworkCore;

namespace AspireBoot.Infrastructure.Integrations.Rabbit;

public class RabbitEventRepository(IDbContextOutbox<AspireBootDbContext> dbContextOutbox) : IEventRepository
{
    public async Task AddAsync(
        IEvent concreteEvent, bool saveChangesAndFlushMessages, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await dbContextOutbox.PublishAsync(concreteEvent).ConfigureAwait(false);

        if (saveChangesAndFlushMessages)
            await dbContextOutbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);
    }
}
