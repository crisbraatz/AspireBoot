using System.Diagnostics;
using System.Diagnostics.Metrics;
using AspireBoot.Infrastructure.Observability;

namespace AspireBoot.Tests.Unit.Infrastructure.Observability;

public class DependencyTraceTests
{
    [Fact]
    public void CancelShouldRecordCancellationAsItsOwnOutcome()
    {
        using ActivitySource activitySource = new(Guid.NewGuid().ToString());
        using ActivityListener activityListener = CreateActivityListener(activitySource);
        List<string?> outcomes = [];
        string dependency = Guid.NewGuid().ToString();
        using MeterListener meterListener = CreateMeterListener(dependency, outcomes, []);
        DependencyTrace dependencyTrace = new(activitySource, dependency, "get");

        dependencyTrace.Cancel();
        dependencyTrace.Dispose();

        Assert.True(Assert.IsType<bool>(dependencyTrace.Activity!.GetTagItem("method.cancelled")));
        Assert.Equal("cancelled", Assert.Single(outcomes));
    }

    [Fact]
    public void ConstructorShouldPreserveAnExplicitParentContextAndActivityKind()
    {
        using ActivitySource activitySource = new(Guid.NewGuid().ToString());
        using ActivityListener activityListener = CreateActivityListener(activitySource);
        ActivityContext activityContext = new(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);

        using DependencyTrace dependencyTrace = new(activitySource, "redis", "get", ActivityKind.Producer, activityContext);

        Activity activity = Assert.IsType<Activity>(dependencyTrace.Activity);
        Assert.Equal(activityContext.TraceId, activity.TraceId);
        Assert.Equal(activityContext.SpanId, activity.ParentSpanId);
        Assert.Equal(ActivityKind.Producer, activity.Kind);
    }

    [Fact]
    public void DisposeShouldRecordASuccessfulOperationOnceWhenDisposedTwice()
    {
        using ActivitySource activitySource = new(Guid.NewGuid().ToString());
        using ActivityListener activityListener = CreateActivityListener(activitySource);
        List<string?> outcomes = [];
        string dependency = Guid.NewGuid().ToString();
        using MeterListener meterListener = CreateMeterListener(dependency, outcomes, []);
        DependencyTrace dependencyTrace = new(activitySource, dependency, "send");
        Activity activity = Assert.IsType<Activity>(dependencyTrace.Activity);

        dependencyTrace.Dispose();
        dependencyTrace.Dispose();

        Assert.Equal(dependency, activity.GetTagItem("dependency.name"));
        Assert.Equal("send", activity.GetTagItem("method.name"));
        Assert.Equal(ActivityKind.Client, activity.Kind);
        Assert.Equal("success", Assert.Single(outcomes));
        Assert.True(activity.IsStopped);
    }

    [Fact]
    public void DisposeShouldRecordTheOutcomeWhenNoActivityListenerExists()
    {
        using ActivitySource activitySource = new(Guid.NewGuid().ToString());
        List<string?> outcomes = [];
        string dependency = Guid.NewGuid().ToString();
        using MeterListener meterListener = CreateMeterListener(dependency, outcomes, []);
        DependencyTrace dependencyTrace = new(activitySource, dependency, "get");

        dependencyTrace.Dispose();

        Assert.Null(dependencyTrace.Activity);
        Assert.Equal("success", Assert.Single(outcomes));
    }

    [Fact]
    public void FailShouldOverrideAnEarlierCancellationWhenNoActivityExists()
    {
        using ActivitySource activitySource = new(Guid.NewGuid().ToString());
        List<string?> outcomes = [];
        List<string?> failures = [];
        string dependency = Guid.NewGuid().ToString();
        using MeterListener meterListener = CreateMeterListener(dependency, outcomes, failures);
        DependencyTrace dependencyTrace = new(activitySource, dependency, "send");

        dependencyTrace.Cancel();
        dependencyTrace.Fail("Unavailable", "Failure.");
        dependencyTrace.Dispose();

        Assert.Null(dependencyTrace.Activity);
        Assert.Equal("failure", Assert.Single(outcomes));
        Assert.Equal("Unavailable", Assert.Single(failures));
    }

    [Fact]
    public void FailShouldPreserveTheFirstFailureWhenFailedOrCancelledAgain()
    {
        using ActivitySource activitySource = new(Guid.NewGuid().ToString());
        using ActivityListener activityListener = CreateActivityListener(activitySource);
        List<string?> outcomes = [];
        List<string?> failures = [];
        string dependency = Guid.NewGuid().ToString();
        using MeterListener meterListener = CreateMeterListener(dependency, outcomes, failures);
        DependencyTrace dependencyTrace = new(activitySource, dependency, "send");

        dependencyTrace.Fail("Unavailable", "Original failure.");
        dependencyTrace.Fail("OtherFailure", "Other description.");
        dependencyTrace.Cancel();
        dependencyTrace.Dispose();

        Assert.Equal(ActivityStatusCode.Error, dependencyTrace.Activity!.Status);
        Assert.Equal("Original failure.", dependencyTrace.Activity.StatusDescription);
        Assert.Equal("Unavailable", dependencyTrace.Activity.GetTagItem("failure.type"));
        Assert.Null(dependencyTrace.Activity.GetTagItem("method.cancelled"));
        Assert.Equal("failure", Assert.Single(outcomes));
        Assert.Equal("Unavailable", Assert.Single(failures));
    }

    private static ActivityListener CreateActivityListener(ActivitySource activitySource)
    {
        ActivityListener activityListener = new() { Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded, ShouldListenTo = sourceActivitySource => ReferenceEquals(sourceActivitySource, activitySource) };
        ActivitySource.AddActivityListener(activityListener);
        return activityListener;
    }

    private static MeterListener CreateMeterListener(string dependency, List<string?> outcomes, List<string?> failures)
    {
        MeterListener meterListener = new() { InstrumentPublished = (instrument, listenerMeterListener) =>
            {
                if (instrument.Meter.Name is "AspireBoot.Infrastructure")
                    listenerMeterListener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((_, _, tagsReadOnlySpan, _) =>
        {
            string? name = null;
            string? outcome = null;
            foreach (KeyValuePair<string, object?> tagKeyValuePair in tagsReadOnlySpan)
            {
                if (tagKeyValuePair.Key is "dependency.name")
                    name = tagKeyValuePair.Value as string;
                if (tagKeyValuePair.Key is "outcome")
                    outcome = tagKeyValuePair.Value as string;
            }
            if (name == dependency)
                outcomes.Add(outcome);
        });
        meterListener.SetMeasurementEventCallback<long>((_, _, tagsTagsReadOnlySpan, _) =>
        {
            string? name = null;
            string? failure = null;
            foreach (KeyValuePair<string, object?> tagTagKeyValuePair in tagsTagsReadOnlySpan)
            {
                if (tagTagKeyValuePair.Key is "dependency.name")
                    name = tagTagKeyValuePair.Value as string;
                if (tagTagKeyValuePair.Key is "failure.type")
                    failure = tagTagKeyValuePair.Value as string;
            }
            if (name == dependency)
                failures.Add(failure);
        });
        meterListener.Start();
        return meterListener;
    }
}
