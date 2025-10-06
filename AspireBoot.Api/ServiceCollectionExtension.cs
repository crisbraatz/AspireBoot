using AspireBoot.Api.Helpers;
using AspireBoot.Api.Services.Auth;
using AspireBoot.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AspireBoot.Api;

public static class ServiceCollectionExtension
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddRouting(x => x.LowercaseUrls = true);
        services.AddJwt();
        services.AddServices();
    }

    private static void AddJwt(this IServiceCollection services) => services
        .AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x => x.TokenValidationParameters = new TokenValidationParameters
        {
            ClockSkew = TimeSpan.Zero,
            IssuerSigningKey = TokenHelper.GetSecurityKey(),
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidAudience = AppSettings.JwtAudience,
            ValidIssuer = AppSettings.JwtIssuer
        });

    public static void AddServices(this IServiceCollection services) => services.AddScoped<IAuthService, AuthService>();
}