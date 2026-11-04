using System.Security.Cryptography.X509Certificates;
using AspireBoot.Core.Integrations.Smtp;
using AspireBoot.Infrastructure.Integrations.Postgres;
using AspireBoot.Infrastructure.Integrations.Smtp;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspireBoot.Infrastructure.Identity;

public static class IdentityDependencyInjection
{
    public static IdentityBuilder AddInfrastructureIdentity(
        this IServiceCollection serviceCollection, IConfiguration configuration, bool isDevelopment)
    {
        SmtpOptions.Add(serviceCollection, configuration, isDevelopment);
        DataProtectionOptions dataProtectionOptions =
            DataProtectionOptions.Get(serviceCollection, configuration, isDevelopment);

        IDataProtectionBuilder dataProtectionBuilder = serviceCollection
            .AddDataProtection().SetApplicationName("AspireBoot").PersistKeysToDbContext<AspireBootDbContext>();

        if (!isDevelopment)
            dataProtectionBuilder.ProtectKeysWithCertificate(X509CertificateLoader.LoadPkcs12FromFile(
                dataProtectionOptions.CertificatePath, dataProtectionOptions.CertificatePassword));

        IdentityBuilder identityBuilder = serviceCollection.AddIdentityCore<IdentityUser>(identityOptions =>
            {
                identityOptions.ClaimsIdentity.EmailClaimType = "email";
                identityOptions.ClaimsIdentity.RoleClaimType = "roles";
                identityOptions.ClaimsIdentity.UserIdClaimType = "sub";
                identityOptions.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                identityOptions.Lockout.MaxFailedAccessAttempts = 5;
                identityOptions.Password.RequiredLength = 12;
                identityOptions.Password.RequiredUniqueChars = 4;
                identityOptions.SignIn.RequireConfirmedEmail = !isDevelopment;
                identityOptions.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AspireBootDbContext>()
            .AddDefaultTokenProviders();
        serviceCollection.AddScoped<IUserStore<IdentityUser>, IdentityUserStore>();
        serviceCollection.Configure<DataProtectionTokenProviderOptions>(dataProtectionTokenProviderOptions =>
            dataProtectionTokenProviderOptions.TokenLifespan = TimeSpan.FromHours(1));
        serviceCollection.AddSingleton<IEmailSender, EmailSender>();

        return identityBuilder;
    }
}
