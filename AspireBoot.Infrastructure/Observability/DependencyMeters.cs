using System.Diagnostics.Metrics;

namespace AspireBoot.Infrastructure.Observability;

public static class DependencyMeters
{
    private static readonly Meter s_meter = new("AspireBoot.Infrastructure");

    public static readonly Histogram<double> DependencyDuration =
        s_meter.CreateHistogram<double>("aspireboot.dependency.duration", "ms");

    public static readonly Counter<long> DependencyFailures =
        s_meter.CreateCounter<long>("aspireboot.dependency.failures", "1");
}
