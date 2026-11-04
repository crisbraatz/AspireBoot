using AspireBoot.Core.Identity.Auth.SendEmailConfirmation;
using Microsoft.AspNetCore.Identity;

namespace AspireBoot.Core.Identity.Users;

public interface IUserRepository
{
    Task<int> CountAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IdentityResult> DeleteAsync(User user, IdentityUser identityUser, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(
        Guid id,
        bool asNoTracking = false,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<User?> FindByIdentityUserIdAsync(
        string identityUserId,
        bool asNoTracking = false,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetAdministratorIdentityUserIdsAsync(
        IReadOnlyList<string> identityUserIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> ListAsync(
        int skip, int take, bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<IdentityResult> TryAddAsync(
        User user,
        IdentityUser identityUser,
        string password,
        SendEmailConfirmationEvent? sendEmailConfirmationEvent = null,
        CancellationToken cancellationToken = default);

    Task<IdentityResult> TryChangePasswordAsync(
        IdentityUser identityUser, string currentPassword, string newPassword, CancellationToken cancellationToken);

    Task<IdentityResult> TryConfirmEmailAsync(
        IdentityUser identityUser, string token, CancellationToken cancellationToken);

    Task<IdentityResult> TryResetPasswordAsync(
        IdentityUser identityUser, string token, string password, CancellationToken cancellationToken);

    Task<IdentityResult> TryUpdateEmailAsync(
        User user, IdentityUser identityUser, string email, string token, CancellationToken cancellationToken);

    Task UpdateAsync(Guid id, IReadOnlyList<string> updatedFields, CancellationToken cancellationToken);
}
