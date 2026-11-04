using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;

namespace AspireBoot.ServiceDefaults;

public sealed class ServiceHeartbeat : IHostedService, IDisposable
{
    private readonly Meter _meter = new("AspireBoot.ServiceDefaults");

    public ServiceHeartbeat() => _meter.CreateObservableGauge("aspireboot.service.heartbeat", () => 1, "1");

    public void Dispose() => _meter.Dispose();

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();

        return Task.CompletedTask;
    }
}
