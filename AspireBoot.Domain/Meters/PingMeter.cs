using System.Diagnostics.Metrics;

namespace AspireBoot.Domain.Meters;

public static class PingMeter
{
    public static Meter Instance { get; } = new(AppSettings.AppName, AppSettings.AppVersion);
    
    public static readonly Counter<int> PingRequests = Instance.CreateCounter<int>("ping_requests");
}