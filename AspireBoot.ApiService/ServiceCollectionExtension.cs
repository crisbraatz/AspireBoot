using System.Text.Json.Serialization;
using AspireBoot.ApiService.Helpers;
using AspireBoot.ApiService.Services.Sessions;
using AspireBoot.ApiService.Services.Users;
using AspireBoot.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AspireBoot.ApiService;

public static class ServiceCollectionExtension
{
    public static void AddApplication(this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
        serviceCollection.AddRouting(x => x.LowercaseUrls = true);
        serviceCollection.AddJwt();
        serviceCollection.AddServices();
    }

    private static void AddJwt(this IServiceCollection serviceCollection) =>
        serviceCollection
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

    public static void AddServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ISessionsService, SessionsService>();
        serviceCollection.AddScoped<IUsersService, UsersService>();
    }
}
