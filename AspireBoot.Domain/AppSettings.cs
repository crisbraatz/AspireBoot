using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace AspireBoot.Domain;

public static class AppSettings
{
    private static IConfiguration? _configuration;

    public static void Get(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public static string AngularOrigin => _configuration?["AspireBoot:AngularOrigin"] ?? "https://localhost:4200";
    public static string AspireOrigin => _configuration?["AspireBoot:AspireOrigin"] ?? "https://localhost:5100";
    private static string AppEnvironment => _configuration?["AspireBoot:AppEnvironment"] ?? "development";
    public static CultureInfo AppLanguage => new(_configuration?["AspireBoot:AppLanguage"] ?? "pt-BR");
    public static string AppName => new(_configuration?["AspireBoot:AppName"] ?? "AspireBoot");
    public static string AppVersion => new(_configuration?["AspireBoot:AppVersion"] ?? "1.0.0");
    public static string CookieDomain => _configuration?["AspireBoot:CookieDomain"] ?? "localhost";
    public static bool IsDevelopment => AppEnvironment is "development";
    public static string JwtAudience => _configuration?["AspireBoot:Jwt.Audience"] ?? "DEFAULT_JWT_AUDIENCE";
    public static string JwtIssuer => _configuration?["AspireBoot:Jwt.Issuer"] ?? "DEFAULT_JWT_ISSUER";

    public static string JwtSecurityKey =>
        _configuration?["AspireBoot:Jwt.SecurityKey"] ?? "DEFAULT_256_BITS_JWT_SECURITY_KEY";

    public static string RabbitPingConsumerExchange =>
        _configuration?["AspireBoot:Rabbit.PingConsumer.Exchange"] ?? "ping-exchange";

    public static string RabbitPingConsumerQueue =>
        _configuration?["AspireBoot:Rabbit.PingConsumer.Queue"] ?? "ping-queue";
}