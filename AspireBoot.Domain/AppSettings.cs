using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace AspireBoot.Domain;

public static class AppSettings
{
    private static IConfiguration? s_configuration;

    public static void Get(IConfiguration configuration) => s_configuration = configuration;

    public static string AngularOrigin => s_configuration?["AspireBoot:AngularOrigin"] ?? "https://localhost:4200";
    private static string AppEnvironment => s_configuration?["AspireBoot:AppEnvironment"] ?? "development";
    public static CultureInfo AppLanguage => new(s_configuration?["AspireBoot:AppLanguage"] ?? "en-US");
    public static string AppName => new(s_configuration?["AspireBoot:AppName"] ?? "AspireBoot");
    public static string AppVersion => new(s_configuration?["AspireBoot:AppVersion"] ?? "1.0.0");
    public static string AspireOrigin => s_configuration?["AspireBoot:AspireOrigin"] ?? "https://localhost:5100";
    public static string CookieDomain => s_configuration?["AspireBoot:Cookie:Domain"] ?? "localhost";

    public static int CookieExpiresAfter
    {
        get
        {
            bool parseOk = int.TryParse(
                s_configuration?["AspireBoot:Cookie:ExpiresAfter"] ?? "7",
                AppLanguage.NumberFormat,
                out int days);

            return parseOk ? days : 7;
        }
    }

    public static bool IsDevelopment => AppEnvironment is "development";
    public static string JwtAudience => s_configuration?["AspireBoot:Jwt:Audience"] ?? "DEFAULT_JWT_AUDIENCE";

    public static int JwtExpiresAfter
    {
        get
        {
            bool parseOk = int.TryParse(
                s_configuration?["AspireBoot:Jwt:ExpiresAfter"] ?? "60",
                AppLanguage.NumberFormat,
                out int minutes);

            return parseOk ? minutes : 60;
        }
    }

    public static string JwtIssuer => s_configuration?["AspireBoot:Jwt:Issuer"] ?? "DEFAULT_JWT_ISSUER";

    public static string JwtSecurityKey =>
        s_configuration?["AspireBoot:Jwt:SecurityKey"] ?? "DEFAULT_256_BITS_JWT_SECURITY_KEY";

    public static string RabbitUserListRequestsCounterConsumerExchange =>
        s_configuration?["AspireBoot:Rabbit:UserListRequestsCounterConsumer:Exchange"] ??
        "user-list-requests-counter-exchange";

    public static string RabbitUserListRequestsCounterConsumerQueue =>
        s_configuration?["AspireBoot:Rabbit:UserListRequestsCounterConsumer:Queue"] ??
        "user-list-requests-counter-queue";
}
