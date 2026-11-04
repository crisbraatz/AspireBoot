namespace AspireBoot.Core.Integrations.Messaging;

public interface IEvent
{
    Guid MessageId { get; }
    string EventType { get; }
    DateTimeOffset CreatedAtUtc { get; }
    int SchemaVersion { get; }
    string? CorrelationId { get; }
    string? TraceParent { get; }
    string? TraceState { get; }
}
