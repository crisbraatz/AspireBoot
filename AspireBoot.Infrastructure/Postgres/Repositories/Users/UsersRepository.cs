using AspireBoot.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace AspireBoot.Infrastructure.Postgres.Repositories.Users;

public class UsersRepository(DatabaseContext context) : BaseEntityRepository<User>(context), IUsersRepository
{
    public async Task<bool> ExistsByAsync(string email, CancellationToken cancellationToken = default) =>
        await DbSet
            .FromSqlInterpolated($"SELECT * FROM users WHERE UPPER(email) = UPPER({email})")
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
}
