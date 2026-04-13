using AspireBoot.Domain.DTOs.Users;
using AspireBoot.Domain.Entities.Users;

namespace AspireBoot.Infrastructure.Postgres.Repositories.Users;

public interface IUsersRepository : IBaseEntityRepository<User>
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<(IEnumerable<ListUserResponseDto> Data, int TotalItems)> ListAsync(
        ListUsersRequestDto request, CancellationToken cancellationToken = default);
}
