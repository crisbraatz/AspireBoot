using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AspireBoot.Api.Identity;
using AspireBoot.Api.Primitives;
using AspireBoot.Core.Identity;
using AspireBoot.Infrastructure.Integrations.Postgres;
using AspireBoot.ServiceDefaults;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using IPNetwork = System.Net.IPNetwork;

namespace AspireBoot.Api;

public static class ApiDependencyInjection
{
    public static void AddApi(
        this IServiceCollection serviceCollection, IConfiguration configuration, bool isDevelopment)
    {
        string[] allowedHosts = (configuration["AllowedHosts"] ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (allowedHosts.Length is 0 || allowedHosts.Contains("*", StringComparer.Ordinal))
            throw new InvalidOperationException("AllowedHosts must list the API hosts.");

        ApiOptions apiOptions = ApiOptions.Get(serviceCollection, configuration);

        serviceCollection.AddProblemDetails(problemDetailsOptions =>
            problemDetailsOptions.CustomizeProblemDetails = problemDetailsContext =>
                problemDetailsContext.ProblemDetails.Extensions["traceId"] =
                    problemDetailsContext.HttpContext.Items.TryGetValue("traceId", out object? traceId)
                        ? traceId
                        : Activity.Current?.TraceId.ToString() ?? problemDetailsContext.HttpContext.TraceIdentifier);
        serviceCollection.AddOpenApi(openApiOptions =>
        {
            openApiOptions.AddOperationTransformer((openApiOperation, openApiOperationTransformerContext, _) =>
            {
                if (openApiOperationTransformerContext.Description.HttpMethod is "GET" or "HEAD" or "OPTIONS")
                    return Task.CompletedTask;

                openApiOperation.Parameters ??= [];
                openApiOperation.Parameters.Add(new OpenApiParameter
                {
                    Description = "Fetch /api/auth/csrf with the current cookie jar before this request.",
                    In = ParameterLocation.Header,
                    Name = "X-CSRF-TOKEN",
                    Required = true,
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                });

                return Task.CompletedTask;
            });
            openApiOptions.AddDocumentTransformer((openApiDocument, _, _) =>
            {
                openApiDocument.Components ??= new OpenApiComponents();
                openApiDocument.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                openApiDocument.Components.SecuritySchemes["Session"] = new OpenApiSecurityScheme
                {
                    Description = "Fetch /api/auth/csrf and send X-CSRF-TOKEN for every write.",
                    In = ParameterLocation.Cookie,
                    Name = "__Host-AspireBoot.Session",
                    Type = SecuritySchemeType.ApiKey
                };

                return Task.CompletedTask;
            });
        });
        serviceCollection.AddApiIdentity(configuration, isDevelopment);
        serviceCollection.AddExceptionHandler<ApiExceptionHandler>();
        serviceCollection.AddExceptionHandler(exceptionHandlerOptions =>
            exceptionHandlerOptions.SuppressDiagnosticsCallback = exceptionHandlerSuppressDiagnosticsContext =>
                exceptionHandlerSuppressDiagnosticsContext.Exception is BadHttpRequestException ||
                exceptionHandlerSuppressDiagnosticsContext.Exception is IOException or OperationCanceledException &&
                exceptionHandlerSuppressDiagnosticsContext.HttpContext.RequestAborted.IsCancellationRequested);
        serviceCollection.Configure<RouteHandlerOptions>(routeHandlerOptions =>
            routeHandlerOptions.ThrowOnBadRequest = true);
        serviceCollection.AddCors(corsOptions =>
            corsOptions.AddPolicy(ApiConstants.CorsPolicyName, corsPolicyBuilder =>
                corsPolicyBuilder
                    .WithOrigins(apiOptions.Frontend.Origin).AllowAnyMethod().AllowAnyHeader().AllowCredentials()));
        serviceCollection.ConfigureHttpJsonOptions(jsonOptions =>
            jsonOptions.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);
        serviceCollection.AddAuthorizationBuilder().AddPolicy(
            ApiConstants.AuthorizationPolicyName,
            authorizationPolicyBuilder =>
                authorizationPolicyBuilder.RequireAuthenticatedUser().RequireRole(IdentityConstants.AdministratorRole));
        serviceCollection.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            rateLimiterOptions.OnRejected = async (onRejectedContext, _) =>
            {
                onRejectedContext.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (onRejectedContext.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                    onRejectedContext.HttpContext.Response.Headers[HeaderNames.RetryAfter] =
                        Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);

                await onRejectedContext.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>()
                    .WriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = onRejectedContext.HttpContext,
                        ProblemDetails = new ProblemDetails
                        {
                            Detail = "Too many requests were sent. Try again later.",
                            Extensions = { ["code"] = "api.rate-limit.exceeded" },
                            Instance = onRejectedContext.HttpContext.Request.Path.Value,
                            Status = StatusCodes.Status429TooManyRequests,
                            Title = "Too many requests",
                            Type = "urn:aspireboot:api.rate-limit.exceeded"
                        }
                    })
                    .ConfigureAwait(false);
            };

            foreach ((string policyName, RateLimiterOptions rateLimiter) in new[]
                     {
                         (ApiConstants.RateLimiterAuthPolicyName, apiOptions.RateLimiters.Auth),
                         (ApiConstants.RateLimiterReadsPolicyName, apiOptions.RateLimiters.Reads),
                         (ApiConstants.RateLimiterWritesPolicyName, apiOptions.RateLimiters.Writes)
                     })
                rateLimiterOptions.AddPolicy(policyName, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                AutoReplenishment = true,
                                PermitLimit = rateLimiter.PermitLimit,
                                QueueLimit = 0,
                                Window = TimeSpan.FromMinutes(rateLimiter.WindowInMinutes)
                            }));
        });
        serviceCollection.Configure<ForwardedHeadersOptions>(forwardedHeadersOptions =>
        {
            forwardedHeadersOptions.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;

            foreach (string allowedHost in allowedHosts)
                forwardedHeadersOptions.AllowedHosts.Add(allowedHost);

            if (!IPNetwork.TryParse(apiOptions.ReverseProxy.IpNetwork, out IPNetwork ipNetwork))
                throw new InvalidOperationException("ReverseProxy IpNetwork must be a valid CIDR network.");

            forwardedHeadersOptions.KnownIPNetworks.Add(ipNetwork);

            if (!isDevelopment)
                return;

            forwardedHeadersOptions.KnownIPNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();
        });
    }

    public static async Task UseApiAsync(this WebApplication webApplication)
    {
        webApplication.UseForwardedHeaders();
        webApplication.UseExceptionHandler();
        webApplication.UseStatusCodePages();
        webApplication.Use(async (httpContext, requestDelegate) =>
        {
            httpContext.Response.OnStarting(() =>
            {
                httpContext.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
                httpContext.Response.Headers["Referrer-Policy"] = "no-referrer";
                httpContext.Response.Headers.XFrameOptions = "DENY";

                if (!httpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                    return Task.CompletedTask;

                httpContext.Response.Headers[HeaderNames.CacheControl] = "no-store";
                httpContext.Response.Headers.ContentSecurityPolicy =
                    "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";

                return Task.CompletedTask;
            });

            await requestDelegate(httpContext).ConfigureAwait(false);
        });

        if (!webApplication.Environment.IsDevelopment())
            webApplication.UseHsts();

        webApplication.UseHttpsRedirection();
        webApplication.UseRouting();
        webApplication.UseCors(ApiConstants.CorsPolicyName);
        webApplication.UseRateLimiter();
        webApplication.UseAuthentication();
        webApplication.UseAuthorization();
        webApplication.Use(async (httpContext, requestDelegate) =>
        {
            if (httpContext.GetEndpoint()?.Metadata.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize
                is not { } maxRequestBodySize)
            {
                await requestDelegate(httpContext).ConfigureAwait(false);

                return;
            }

            if (httpContext.Request.ContentLength > maxRequestBodySize)
                throw new BadHttpRequestException("Request body is too large.", StatusCodes.Status413PayloadTooLarge);

            IHttpMaxRequestBodySizeFeature? httpMaxRequestBodySizeFeature =
                httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();

            if (httpMaxRequestBodySizeFeature is { IsReadOnly: false })
                httpMaxRequestBodySizeFeature.MaxRequestBodySize = maxRequestBodySize;

            if (httpContext.Request.ContentLength is not null ||
                HttpMethods.IsGet(httpContext.Request.Method) ||
                HttpMethods.IsHead(httpContext.Request.Method))
            {
                await requestDelegate(httpContext).ConfigureAwait(false);

                return;
            }

            httpContext.Request.EnableBuffering();
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);

            try
            {
                long length = 0;
                int read;

                do
                {
                    read = await httpContext.Request.Body
                        .ReadAsync(
                            buffer.AsMemory(0, (int)Math.Min(buffer.Length, maxRequestBodySize - length + 1)),
                            httpContext.RequestAborted)
                        .ConfigureAwait(false);

                    length += read;

                    if (length > maxRequestBodySize)
                        throw new BadHttpRequestException(
                            "Request body is too large.", StatusCodes.Status413PayloadTooLarge);
                } while (read > 0);

                httpContext.Request.Body.Position = 0;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, true);
            }

            await requestDelegate(httpContext).ConfigureAwait(false);
        });
        webApplication.UseAntiforgery();
        webApplication.Use(async (httpContext, requestDelegate) =>
        {
            if (!httpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
                HttpMethods.IsGet(httpContext.Request.Method) ||
                HttpMethods.IsHead(httpContext.Request.Method) ||
                HttpMethods.IsOptions(httpContext.Request.Method))
            {
                await requestDelegate(httpContext).ConfigureAwait(false);

                return;
            }

            try
            {
                await httpContext.RequestServices.GetRequiredService<IAntiforgery>()
                    .ValidateRequestAsync(httpContext).ConfigureAwait(false);
            }
            catch (AntiforgeryValidationException)
            {
                await Results.Problem(
                        extensions: new Dictionary<string, object?> { ["code"] = "identity.auth.csrf.invalid" },
                        statusCode: 400,
                        title: "Invalid CSRF token")
                    .ExecuteAsync(httpContext).ConfigureAwait(false);

                return;
            }

            await requestDelegate(httpContext).ConfigureAwait(false);
        });
        webApplication.MapDefaultEndpoints();
        webApplication.MapApiEndpoints();

        if (webApplication.Environment.IsDevelopment())
        {
            webApplication.MapOpenApi();
            webApplication.MapScalarApiReference();
        }

        if (webApplication.Services.GetRequiredService<PostgresOptions>().ApplyMigrationsOnStartup is not true)
            return;

        using IServiceScope serviceScope = webApplication.Services.CreateScope();

        await serviceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>()
            .Database.MigrateAsync().ConfigureAwait(false);
    }
}
