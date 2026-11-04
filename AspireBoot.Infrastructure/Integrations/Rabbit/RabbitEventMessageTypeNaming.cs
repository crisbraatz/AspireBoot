using System.Reflection;
using AspireBoot.Core.Integrations.Messaging;
using Wolverine.Util;

namespace AspireBoot.Infrastructure.Integrations.Rabbit;

public class RabbitEventMessageTypeNaming : IMessageTypeNaming
{
    public bool TryDetermineName(Type messageType, out string messageTypeName)
    {
        messageTypeName = messageType.GetCustomAttribute<EventTypeAttribute>()?.EventType ?? string.Empty;

        return !string.IsNullOrEmpty(messageTypeName);
    }
}
