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

public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static void AddServiceDefaults<TBuilder>(this TBuilder builder, string activitySourceName)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry(activitySourceName);
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(x =>
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
                .AddMeter(PingMeter.Instance.Name))
            .WithTracing(x => x.AddSource(activitySourceName)
                .AddAspNetCoreInstrumentation(y => y.Filter = z =>
                    !z.Request.Path.StartsWithSegments(HealthEndpointPath) &&
                    !z.Request.Path.StartsWithSegments(AlivenessEndpointPath))
                .AddHttpClientInstrumentation());
        builder.AddOpenTelemetryExporters();
    }

    private static void AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
    }

    private static void AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder =>
        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

    public static void MapDefaultEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        app.MapHealthChecks(HealthEndpointPath);
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions { Predicate = x => x.Tags.Contains("live") });
    }
}