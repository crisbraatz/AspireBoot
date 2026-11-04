using AspireBoot.Core.Identity.Auth.ConfirmEmail;
using AspireBoot.Core.Identity.Auth.PasswordChanged;
using AspireBoot.Core.Identity.Auth.SendEmailConfirmation;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Identity.Users.Audit;
using AspireBoot.Core.Identity.Users.DeleteUser;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Wolverine.EntityFrameworkCore;
using IdentityConstants = AspireBoot.Core.Identity.IdentityConstants;

namespace AspireBoot.Infrastructure.Identity.Users;

public class UserRepository(
    AspireBootDbContext aspireBootDbContext,
    IDbContextOutbox<AspireBootDbContext> dbContextOutbox,
    UserManager<IdentityUser> userManager)
    : IUserRepository
{
    public async Task<int> CountAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        IQueryable<User> queryable = aspireBootDbContext.Users.AsQueryable();

        if (!includeInactive)
            queryable = queryable.Where(user => user.IsActive);

        return await queryable.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IdentityResult> DeleteAsync(
        User user, IdentityUser identityUser, CancellationToken cancellationToken)
    {
        IDbContextTransaction dbContextTransaction =
            await aspireBootDbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (dbContextTransaction.ConfigureAwait(false))
        {
            identityUser.LockoutEnabled = true;

            IdentityResult identityResult =
                await userManager.SetLockoutEndDateAsync(identityUser, DateTimeOffset.MaxValue).ConfigureAwait(false);

            if (!identityResult.Succeeded)
                return identityResult;

            identityResult = await userManager.UpdateSecurityStampAsync(identityUser).ConfigureAwait(false);

            if (!identityResult.Succeeded)
                return identityResult;

            user.Deactivate();

            await dbContextOutbox
                .PublishAsync(new DeleteUserEvent(identityUser.Id, identityUser.Email!, DateTimeOffset.UtcNow))
                .ConfigureAwait(false);
            await dbContextOutbox
                .PublishAsync(new UserAuditEvent(
                    user.Id, UserAuditEvent.Update, [UserAuditFields.IsActive, UserAuditFields.UpdatedAtUtc]))
                .ConfigureAwait(false);
            await dbContextOutbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);

            return IdentityResult.Success;
        }
    }

    public async Task<User?> FindByIdAsync(
        Guid id, bool asNoTracking = false, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        IQueryable<User> queryable = aspireBootDbContext.Users.AsQueryable();

        if (asNoTracking)
            queryable = queryable.AsNoTracking();

        return await queryable
            .SingleOrDefaultAsync(user => user.Id == id && (includeInactive || user.IsActive), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<User?> FindByIdentityUserIdAsync(
        string identityUserId,
        bool asNoTracking = false,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> queryable = aspireBootDbContext.Users.AsQueryable();

        if (asNoTracking)
            queryable = queryable.AsNoTracking();

        return await queryable
            .SingleOrDefaultAsync(
                user => user.IdentityUserId == identityUserId && (includeInactive || user.IsActive), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> GetAdministratorIdentityUserIdsAsync(
        IReadOnlyList<string> identityUserIds, CancellationToken cancellationToken)
    {
        if (identityUserIds.Count is 0)
            return [];

        return await
            (from userRole in aspireBootDbContext.UserRoles.AsNoTracking()
                join role in aspireBootDbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where
                    identityUserIds.Contains(userRole.UserId) &&
                    role.NormalizedName == userManager.NormalizeName(IdentityConstants.AdministratorRole)
                select userRole.UserId)
            .Distinct()
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<User>> ListAsync(
        int skip, int take, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        IQueryable<User> queryable = aspireBootDbContext.Users.AsNoTracking();

        if (!includeInactive)
            queryable = queryable.Where(user => user.IsActive);

        return await queryable
            .OrderBy(user => user.Name)
            .ThenBy(user => user.Id)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IdentityResult> TryAddAsync(
        User user,
        IdentityUser identityUser,
        string password,
        SendEmailConfirmationEvent? sendEmailConfirmationEvent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IDbContextTransaction dbContextTransaction =
                await aspireBootDbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await using (dbContextTransaction.ConfigureAwait(false))
            {
                IdentityResult identityResult =
                    await userManager.CreateAsync(identityUser, password).ConfigureAwait(false);

                if (!identityResult.Succeeded)
                    return identityResult;

                await aspireBootDbContext.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);

                if (sendEmailConfirmationEvent is null)
                {
                    await aspireBootDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    await dbContextTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                    return identityResult;
                }

                await dbContextOutbox.PublishAsync(sendEmailConfirmationEvent).ConfigureAwait(false);
                await dbContextOutbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);

                return identityResult;
            }
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is PostgresException
             {
                 ConstraintName: UserConfiguration.IdentityIndexName,
                 SqlState: PostgresErrorCodes.UniqueViolation
             })
        {
            aspireBootDbContext.Entry(user).State = EntityState.Detached;

            return IdentityResult.Failed(
                new IdentityError { Code = "unique-violation", Description = "Unique violation." });
        }
    }

    public Task<IdentityResult> TryChangePasswordAsync(
        IdentityUser identityUser, string currentPassword, string newPassword, CancellationToken cancellationToken) =>
        ChangePasswordAsync(
            identityUser,
            () => userManager.ChangePasswordAsync(identityUser, currentPassword, newPassword), cancellationToken);

    public async Task<IdentityResult> TryConfirmEmailAsync(
        IdentityUser identityUser, string token, CancellationToken cancellationToken)
    {
        IDbContextTransaction dbContextTransaction =
            await aspireBootDbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (dbContextTransaction.ConfigureAwait(false))
        {
            bool emailConfirmed = identityUser.EmailConfirmed;

            IdentityResult identityResult =
                await userManager.ConfirmEmailAsync(identityUser, token).ConfigureAwait(false);

            if (!identityResult.Succeeded)
                return identityResult;

            if (!emailConfirmed)
                await dbContextOutbox
                    .PublishAsync(new ConfirmEmailEvent(identityUser.Id, identityUser.Email!)).ConfigureAwait(false);

            await dbContextOutbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);

            return identityResult;
        }
    }

    public Task<IdentityResult> TryResetPasswordAsync(
        IdentityUser identityUser, string token, string password, CancellationToken cancellationToken) =>
        ChangePasswordAsync(
            identityUser, () => userManager.ResetPasswordAsync(identityUser, token, password), cancellationToken);

    public async Task<IdentityResult> TryUpdateEmailAsync(
        User user, IdentityUser identityUser, string email, string token, CancellationToken cancellationToken)
    {
        try
        {
            IDbContextTransaction dbContextTransaction =
                await aspireBootDbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await using (dbContextTransaction.ConfigureAwait(false))
            {
                if (!user.ChangeEmail(email))
                    return IdentityResult.Failed(
                        new IdentityError { Code = "equal-violation", Description = "Equal violation." });

                IdentityResult identityResult =
                    await userManager.ChangeEmailAsync(identityUser, email, token).ConfigureAwait(false);

                if (!identityResult.Succeeded)
                    return identityResult;

                await dbContextOutbox.PublishAsync(
                        new UserAuditEvent(
                            user.Id,
                            UserAuditEvent.Update,
                            [UserAuditFields.Email, UserAuditFields.UpdatedAtUtc]))
                    .ConfigureAwait(false);

                await dbContextOutbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);

                return identityResult;
            }
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is PostgresException
             {
                 ConstraintName: UserConfiguration.IdentityIndexName,
                 SqlState: PostgresErrorCodes.UniqueViolation
             })
        {
            aspireBootDbContext.Entry(user).State = EntityState.Detached;

            return IdentityResult.Failed(
                new IdentityError { Code = "unique-violation", Description = "Unique violation." });
        }
    }

    public async Task UpdateAsync(Guid id, IReadOnlyList<string> updatedFields, CancellationToken cancellationToken)
    {
        await dbContextOutbox
            .PublishAsync(new UserAuditEvent(id, UserAuditEvent.Update, updatedFields)).ConfigureAwait(false);
        await dbContextOutbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IdentityResult> ChangePasswordAsync(
        IdentityUser identityUser, Func<Task<IdentityResult>> func, CancellationToken cancellationToken)
    {
        IDbContextTransaction dbContextTransaction =
            await aspireBootDbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (dbContextTransaction.ConfigureAwait(false))
        {
            IdentityResult identityResult = await func().ConfigureAwait(false);

            if (!identityResult.Succeeded)
                return identityResult;

            await dbContextOutbox
                .PublishAsync(new PasswordChangedEvent(identityUser.Id, identityUser.Email!)).ConfigureAwait(false);
            await dbContextOutbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);

            return identityResult;
        }
    }
}
