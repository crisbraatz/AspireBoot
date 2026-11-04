namespace AspireBoot.Core.Integrations.Messaging;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EventTypeAttribute(string eventType) : Attribute
{
    public string EventType { get; } =
        !string.IsNullOrWhiteSpace(eventType) ? eventType : throw new ArgumentNullException(nameof(eventType));
}
