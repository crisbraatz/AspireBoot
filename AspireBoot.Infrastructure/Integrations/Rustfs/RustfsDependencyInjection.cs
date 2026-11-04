using Amazon.Runtime;
using Amazon.S3;
using AspireBoot.Core.Integrations.ObjectStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AspireBoot.Infrastructure.Integrations.Rustfs;

public static class RustfsDependencyInjection
{
    public static void AddRustfs(
        this IServiceCollection serviceCollection, IConfiguration configuration, bool isDevelopment)
    {
        RustfsOptions rustfsOptions = RustfsOptions.Get(serviceCollection, configuration);

        if (!isDevelopment && rustfsOptions.CreateBucketIfMissing)
            throw new InvalidOperationException(
                "ObjectStorage CreateBucketIfMissing creation may only be enabled in Development.");

        Uri endpoint = GetRustfsEndpoint(rustfsOptions.Endpoint, nameof(rustfsOptions.Endpoint));
        Uri publicEndpoint =
            GetRustfsEndpoint(rustfsOptions.PublicEndpoint, nameof(rustfsOptions.PublicEndpoint));

        if (!isDevelopment && publicEndpoint.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("ObjectStorage PublicEndpoint must use HTTPS outside Development.");

        BasicAWSCredentials basicAwsCredentials = new(rustfsOptions.AccessKey, rustfsOptions.SecretKey);

        serviceCollection.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
            basicAwsCredentials,
            new AmazonS3Config
            {
                AuthenticationRegion = rustfsOptions.Region,
                ForcePathStyle = true,
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
                ServiceURL = endpoint.GetLeftPart(UriPartial.Authority)
            }));
        serviceCollection.AddKeyedSingleton<IAmazonS3>("Rustfs", (serviceProvider, _) =>
            publicEndpoint == endpoint
                ? serviceProvider.GetRequiredService<IAmazonS3>()
                : new AmazonS3Client(
                    basicAwsCredentials,
                    new AmazonS3Config
                    {
                        AuthenticationRegion = rustfsOptions.Region,
                        ForcePathStyle = true,
                        RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                        ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
                        ServiceURL = publicEndpoint.GetLeftPart(UriPartial.Authority)
                    }));
        serviceCollection.AddSingleton<IObjectStorage, RustfsObjectStorage>();
        serviceCollection.AddHealthChecks().AddUrlGroup(
            new Uri(endpoint, rustfsOptions.ReadinessPath),
            "rustfs",
            HealthStatus.Unhealthy,
            ["ready", "dependency", "rustfs"]);
    }

    private static Uri GetRustfsEndpoint(string endpoint, string optionName)
    {
        Uri uri = new(endpoint, UriKind.Absolute);

        if (uri.Scheme is not "http" and not "https" ||
            uri.AbsolutePath is not "/" ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException(
                $"ObjectStorage {optionName} must be an HTTPS origin without path, credentials, query or fragment.");

        return uri;
    }
}
