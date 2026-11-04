using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AspireBoot.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    private const string AlivenessEndpointPath = "/alive";
    private const string HealthEndpointPath = "/health";

    private static readonly string[] s_otlpEndpointKeys =
    [
        "OTEL_EXPORTER_OTLP_ENDPOINT",
        "OTEL_EXPORTER_OTLP_METRICS_ENDPOINT",
        "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT",
        "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"
    ];

    public static void AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        Resource resource = ResourceBuilder.CreateDefault()
            .AddService(
                builder.Configuration["OTEL_SERVICE_NAME"] ?? builder.Environment.ApplicationName,
                serviceVersion: Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion,
                serviceInstanceId: Guid.CreateVersion7().ToString())
            .AddAttributes([
                new KeyValuePair<string, object>("deployment.environment.name", builder.Environment.EnvironmentName)
            ])
            .AddEnvironmentVariableDetector()
            .Build();
        builder.Logging.Configure(loggerFactoryOptions => loggerFactoryOptions.ActivityTrackingOptions =
            ActivityTrackingOptions.ParentId | ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId);

        if (!builder.Environment.IsDevelopment())
            builder.Logging.AddJsonConsole(jsonConsoleFormatterOptions =>
                jsonConsoleFormatterOptions.IncludeScopes = true);

        builder.Logging.AddOpenTelemetry(openTelemetryLoggerOptions =>
        {
            openTelemetryLoggerOptions.IncludeFormattedMessage = true;
            openTelemetryLoggerOptions.IncludeScopes = true;
            openTelemetryLoggerOptions.SetResourceBuilder(
                ResourceBuilder.CreateEmpty().AddAttributes(resource.Attributes));
        });

        Uri.TryCreate(
            builder.Configuration["AspireBoot:Infrastructure:ObjectStorage:Endpoint"],
            UriKind.Absolute,
            out Uri? objectStorageEndpoint);
        Uri.TryCreate(
            builder.Configuration["AspireBoot:Infrastructure:ObjectStorage:PublicEndpoint"],
            UriKind.Absolute,
            out Uri? objectStoragePublicEndpoint);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resourceBuilder => resourceBuilder.AddAttributes(resource.Attributes))
            .WithMetrics(meterProviderBuilder =>
                meterProviderBuilder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddNpgsqlInstrumentation(_ => { })
                    .AddRuntimeInstrumentation()
                    .AddMeter("AspireBoot.Infrastructure")
                    .AddMeter("AspireBoot.ServiceDefaults")
                    .AddMeter("Wolverine*"))
            .WithTracing(tracerProviderBuilder =>
                tracerProviderBuilder
                    .AddSource(builder.Environment.ApplicationName)
                    .AddSource("AspireBoot.Infrastructure.Caching")
                    .AddSource("AspireBoot.Infrastructure.ObjectStorage")
                    .AddSource("AspireBoot.Infrastructure.Smtp")
                    .AddSource("Wolverine")
                    .AddAspNetCoreInstrumentation(aspNetCoreTraceInstrumentationOptions =>
                        aspNetCoreTraceInstrumentationOptions.Filter = httpContext =>
                            !httpContext.Request.Path
                                .StartsWithSegments(AlivenessEndpointPath, StringComparison.OrdinalIgnoreCase) &&
                            !httpContext.Request.Path
                                .StartsWithSegments(HealthEndpointPath, StringComparison.OrdinalIgnoreCase))
                    .AddHttpClientInstrumentation(httpClientTraceInstrumentationOptions =>
                        httpClientTraceInstrumentationOptions.FilterHttpRequestMessage = httpRequestMessage =>
                            httpRequestMessage.RequestUri is not { } requestUri ||
                            requestUri.GetLeftPart(UriPartial.Authority) !=
                            objectStorageEndpoint?.GetLeftPart(UriPartial.Authority) &&
                            requestUri.GetLeftPart(UriPartial.Authority) !=
                            objectStoragePublicEndpoint?.GetLeftPart(UriPartial.Authority))
                    .AddNpgsql());

        if (s_otlpEndpointKeys.Any(otlpEndpointKey =>
                !string.IsNullOrWhiteSpace(builder.Configuration[otlpEndpointKey])))
            builder.Services.AddOpenTelemetry().UseOtlpExporter();

        builder.Services.AddHostedService<ServiceHeartbeat>();
        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(httpClientBuilder =>
        {
            httpClientBuilder.AddStandardResilienceHandler(httpStandardResilienceOptions =>
                httpStandardResilienceOptions.Retry.DisableForUnsafeHttpMethods());
            httpClientBuilder.AddServiceDiscovery();
        });
    }

    public static void MapDefaultEndpoints(this WebApplication webApplication)
    {
        webApplication.MapHealthChecks(
            AlivenessEndpointPath,
            new HealthCheckOptions
            {
                Predicate = healthCheckRegistration => healthCheckRegistration.Tags.Contains("live")
            });
        webApplication.MapHealthChecks(HealthEndpointPath);
    }
}
