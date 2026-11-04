using AspireBoot.Core.Identity.Sessions;
using AspireBoot.Infrastructure.Identity;
using AspireBoot.Infrastructure.Integrations.Redis;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Api.Identity;

public static class IdentityDependencyInjection
{
    public static void AddApiIdentity(
        this IServiceCollection serviceCollection, IConfiguration configuration, bool isDevelopment)
    {
        serviceCollection.AddInfrastructureIdentity(configuration, isDevelopment).AddSignInManager();
        serviceCollection.Configure<SecurityStampValidatorOptions>(securityStampValidatorOptions =>
            securityStampValidatorOptions.ValidationInterval = TimeSpan.Zero);
        serviceCollection.AddSingleton<ISessionsTicketStore, RedisSessionsTicketStore>();
        serviceCollection.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddCookie(IdentityConstants.ApplicationScheme, cookieAuthenticationOptions =>
            {
                cookieAuthenticationOptions.Cookie.HttpOnly = true;
                cookieAuthenticationOptions.Cookie.Name = "__Host-AspireBoot.Session";
                cookieAuthenticationOptions.Cookie.Path = "/";
                cookieAuthenticationOptions.Cookie.SameSite = SameSiteMode.Strict;
                cookieAuthenticationOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                cookieAuthenticationOptions.Events.OnRedirectToAccessDenied = redirectContext =>
                {
                    redirectContext.Response.StatusCode = StatusCodes.Status403Forbidden;

                    return Task.CompletedTask;
                };
                cookieAuthenticationOptions.Events.OnRedirectToLogin = redirectContext =>
                {
                    redirectContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

                    return Task.CompletedTask;
                };
                cookieAuthenticationOptions.Events.OnSigningIn = context =>
                {
                    if (context.HttpContext.Items.TryGetValue(
                            Core.Identity.IdentityConstants.SessionTrackedDetailsProperty,
                            out object? sessionTrackedDetailsProperty) &&
                        sessionTrackedDetailsProperty is string sessionTrackedDetails)
                        context.Properties.Items[Core.Identity.IdentityConstants.SessionTrackedDetailsProperty] =
                            sessionTrackedDetails;

                    return Task.CompletedTask;
                };
                cookieAuthenticationOptions.Events.OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync;
                cookieAuthenticationOptions.ExpireTimeSpan = TimeSpan.FromHours(24);
                cookieAuthenticationOptions.SlidingExpiration = true;
            })
            .AddCookie(IdentityConstants.TwoFactorRememberMeScheme, cookieAuthenticationOptions =>
            {
                cookieAuthenticationOptions.Cookie.HttpOnly = true;
                cookieAuthenticationOptions.Cookie.Name = "__Host-AspireBoot.TwoFactor";
                cookieAuthenticationOptions.Cookie.Path = "/";
                cookieAuthenticationOptions.Cookie.SameSite = SameSiteMode.Strict;
                cookieAuthenticationOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            })
            .AddCookie(IdentityConstants.TwoFactorUserIdScheme, cookieAuthenticationOptions =>
            {
                cookieAuthenticationOptions.Cookie.HttpOnly = true;
                cookieAuthenticationOptions.Cookie.Name = "__Host-AspireBoot.TwoFactorPending";
                cookieAuthenticationOptions.Cookie.Path = "/";
                cookieAuthenticationOptions.Cookie.SameSite = SameSiteMode.Strict;
                cookieAuthenticationOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                cookieAuthenticationOptions.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            });
        serviceCollection.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
            .Configure<ISessionsTicketStore>((cookieAuthenticationOptions, redisTicketStore) =>
                cookieAuthenticationOptions.SessionStore = redisTicketStore);
        serviceCollection.AddAntiforgery(antiForgeryOptions =>
        {
            antiForgeryOptions.Cookie.HttpOnly = true;
            antiForgeryOptions.Cookie.Name = "__Host-AspireBoot.Antiforgery";
            antiForgeryOptions.Cookie.Path = "/";
            antiForgeryOptions.Cookie.SameSite = SameSiteMode.Strict;
            antiForgeryOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            antiForgeryOptions.HeaderName = "X-CSRF-TOKEN";
        });
    }
}
