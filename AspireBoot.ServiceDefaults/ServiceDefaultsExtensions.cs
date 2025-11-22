using AspireBoot.Domain.Meters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace AspireBoot.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static void AddServiceDefaults<TBuilder>(this TBuilder builder, string activitySourceName)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry(activitySourceName);
        builder.AddDefaultHealthChecks();
        builder.Services
            .AddServiceDiscovery()
            .ConfigureHttpClientDefaults(x =>
            {
                x.AddStandardResilienceHandler();
                x.AddServiceDiscovery();
            });
    }

    private static void ConfigureOpenTelemetry<TBuilder>(this TBuilder builder, string activitySourceName)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(x =>
        {
            x.IncludeFormattedMessage = true;
            x.IncludeScopes = true;
        });
        builder.Services.AddOpenTelemetry()
            .WithMetrics(x => x
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(UsersMeter.Meter.Name))
            .WithTracing(x => x
                .AddSource(activitySourceName)
                .AddAspNetCoreInstrumentation(y => y.Filter = z =>
                    !z.Request.Path.StartsWithSegments(HealthEndpointPath, StringComparison.OrdinalIgnoreCase)
                    && !z.Request.Path.StartsWithSegments(AlivenessEndpointPath, StringComparison.OrdinalIgnoreCase))
                .AddHttpClientInstrumentation());
        builder.AddOpenTelemetryExporters();
    }

    private static void AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
    }

    private static void AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        => builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

    public static void MapDefaultEndpoints(this WebApplication webApplication)
    {
        if (!webApplication.Environment.IsDevelopment())
            return;

        webApplication.MapHealthChecks(HealthEndpointPath);
        webApplication.MapHealthChecks(
            AlivenessEndpointPath, new HealthCheckOptions { Predicate = x => x.Tags.Contains("live") });
    }
}
