namespace AspireBoot.Infrastructure.Observability;

#pragma warning disable CA1032
public class DependencyUnavailableException(DependencyKind dependencyKind, Exception exception)
#pragma warning restore CA1032
    : Exception("A required dependency is temporarily unavailable.", exception)
{
    public DependencyKind DependencyKind { get; } = dependencyKind;
}
