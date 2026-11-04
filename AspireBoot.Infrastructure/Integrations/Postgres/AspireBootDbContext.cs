using AspireBoot.Core.Identity.Users;
using AspireBoot.Infrastructure.Identity;
using AspireBoot.Infrastructure.Identity.Users.Audit;
using AspireBoot.Infrastructure.Integrations.Rabbit;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace AspireBoot.Infrastructure.Integrations.Postgres;

public class AspireBootDbContext(DbContextOptions<AspireBootDbContext> dbContextOptions)
    : IdentityDbContext<IdentityUser>(dbContextOptions), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<UserAudit> UserAudits => Set<UserAudit>();
    public new DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("citext");
        builder.ConfigureIdentityUser();
        builder.ApplyConfigurationsFromAssembly(typeof(AspireBootDbContext).Assembly);
        builder.MapWolverineEnvelopeStorage(RabbitDependencyInjection.WolverineDatabaseSchema);
    }
}
