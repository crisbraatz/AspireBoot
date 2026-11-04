using Microsoft.Extensions.Logging;

namespace AspireBoot.Infrastructure.Observability;

public static partial class DependencyLogs
{
    [LoggerMessage(
        LogLevel.Error, "Dependency {DependencyKind} method {Method} failed {FailureType}: {FailureDescription}")]
    public static partial void LogDependencyFailure(
        ILogger logger,
        DependencyKind dependencyKind,
        string method,
        string failureType,
        string failureDescription,
        Exception? e = null);

    [LoggerMessage(
        LogLevel.Warning, "Dependency {DependencyKind} method {Method} warned {WarningType}: {WarningDescription}")]
    public static partial void LogDependencyWarning(
        ILogger logger, DependencyKind dependencyKind, string method, string warningType, string warningDescription);
}
