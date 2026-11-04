using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AspireBoot.Infrastructure.Identity;

public class IdentityUserStore(AspireBootDbContext aspireBootDbContext, IdentityErrorDescriber identityErrorDescriber)
    : UserStore<IdentityUser, IdentityRole, AspireBootDbContext>(aspireBootDbContext, identityErrorDescriber)
{
    public override async Task<IdentityResult> CreateAsync(
        IdentityUser identityUser, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.CreateAsync(identityUser, cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is PostgresException
             {
                 ConstraintName: "EmailIndex",
                 SqlState: PostgresErrorCodes.UniqueViolation
             })
        {
            Context.Entry(identityUser).State = EntityState.Detached;

            return IdentityResult.Failed(ErrorDescriber.DuplicateEmail(identityUser.Email!));
        }
    }

    public override async Task<IdentityResult> UpdateAsync(
        IdentityUser identityUser, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.UpdateAsync(identityUser, cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is PostgresException
             {
                 ConstraintName: "EmailIndex",
                 SqlState: PostgresErrorCodes.UniqueViolation
             })
        {
            Context.Entry(identityUser).State = EntityState.Detached;

            return IdentityResult.Failed(ErrorDescriber.DuplicateEmail(identityUser.Email!));
        }
    }
}
