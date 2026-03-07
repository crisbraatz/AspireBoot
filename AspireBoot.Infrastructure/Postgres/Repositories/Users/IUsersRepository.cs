using AspireBoot.Domain.Entities.Users;

namespace AspireBoot.Infrastructure.Postgres.Repositories.Users;

public interface IUsersRepository : IBaseEntityRepository<User>
{
    Task<bool> ExistsByAsync(string email, CancellationToken cancellationToken = default);
}
