using System.Diagnostics;
using System.Reflection;

namespace AspireBoot.Core.Integrations.Messaging;

public abstract record AbstractEvent : IEvent
{
    public Guid MessageId { get; init; } = Guid.CreateVersion7();

    public string EventType =>
        GetType().GetCustomAttribute<EventTypeAttribute>()?.EventType ??
        throw new InvalidOperationException($"Event {GetType().Name} must declare {nameof(EventTypeAttribute)}");

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public virtual int SchemaVersion => 1;
    public string? CorrelationId { get; init; } = Activity.Current?.TraceId.ToString();
    public string? TraceParent { get; init; } = Activity.Current?.Id;
    public string? TraceState { get; init; } = Activity.Current?.TraceStateString;
}
