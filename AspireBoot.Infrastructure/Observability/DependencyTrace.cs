using System.Diagnostics;

namespace AspireBoot.Infrastructure.Observability;

public sealed class DependencyTrace : IDisposable
{
    public Activity? Activity { get; }
    private readonly string _dependency;
    private int _disposed;
    private readonly string _method;
    private string _outcome = "success";
    private readonly long _startedAtTimestamp = Stopwatch.GetTimestamp();

    public DependencyTrace(
        ActivitySource activitySource,
        string dependency,
        string method,
        ActivityKind activityKind = ActivityKind.Client,
        ActivityContext? activityContext = null)
    {
        Activity = activitySource.StartActivity($"{dependency}.{method}", activityKind, activityContext ?? default);
        _dependency = dependency;
        _method = method;
        Activity?.SetTag("dependency.name", dependency);
        Activity?.SetTag("method.name", method);
    }

    public void Cancel()
    {
        if (_outcome is "failure")
            return;

        _outcome = "cancelled";
        Activity?.SetTag("method.cancelled", true);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) is 1)
            return;

        TagList tagList = new()
        {
            { "dependency.name", _dependency }, { "method.name", _method }, { "outcome", _outcome }
        };
        DependencyMeters.DependencyDuration.Record(
            Stopwatch.GetElapsedTime(_startedAtTimestamp).TotalMilliseconds, in tagList);
        Activity?.Dispose();
    }

    public void Fail(string failureType, string failureDescription)
    {
        if (_outcome is "failure")
            return;

        _outcome = "failure";
        Activity?.SetStatus(ActivityStatusCode.Error, failureDescription);
        Activity?.SetTag("failure.type", failureType);
        TagList tagList = new()
        {
            { "dependency.name", _dependency }, { "method.name", _method }, { "failure.type", failureType }
        };
        DependencyMeters.DependencyFailures.Add(1, in tagList);
    }
}
