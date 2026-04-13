using System.Linq.Expressions;
using AspireBoot.Domain.DTOs.Users;
using AspireBoot.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace AspireBoot.Infrastructure.Postgres.Repositories.Users;

public class UsersRepository(DatabaseContext context) : BaseEntityRepository<User>(context), IUsersRepository
{
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .AnyAsync(x => EF.Functions.ILike(x.Email, email), cancellationToken)
            .ConfigureAwait(false);

    public async Task<(IEnumerable<ListUserResponseDto> Data, int TotalItems)> ListAsync(
        ListUsersRequestDto request, CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = DbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Email))
            query = query.Where(x => EF.Functions.ILike(x.Email, $"%{request.Email}%"));

        int totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        query = ApplySorting(query, request);
        List<ListUserResponseDto> data = await query
            .Skip((request.CurrentPage - 1) * request.Size)
            .Take(request.Size)
            .Select(ToListUserResponse())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (data, totalItems);
    }

#pragma warning disable CA1866
    private static IQueryable<User> ApplySorting(IQueryable<User> query, ListUsersRequestDto request) =>
        (request.SortBy ?? string.Empty).ToUpperInvariant() switch
        {
            "CREATEDAT" => request.SortDescending
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt),
            "UPDATEDAT" => request.SortDescending
                ? query.OrderByDescending(x => x.UpdatedAt)
                : query.OrderBy(x => x.UpdatedAt),
            _ => request.SortDescending
                ? query
                    .OrderByDescending(x => x.Email.Substring(0, x.Email.IndexOf("@")))
                    .ThenByDescending(x => x.Email)
                : query
                    .OrderBy(x => x.Email.Substring(0, x.Email.IndexOf("@")))
                    .ThenBy(x => x.Email)
        };
#pragma warning restore CA1866

    private static Expression<Func<User, ListUserResponseDto>> ToListUserResponse() => x => new ListUserResponseDto
    {
        Email = x.Email,
        Id = x.Id,
        CreatedAt = x.CreatedAt,
        CreatedBy = x.CreatedBy,
        UpdatedAt = x.UpdatedAt,
        UpdatedBy = x.UpdatedBy,
        Active = x.Active
    };
}
