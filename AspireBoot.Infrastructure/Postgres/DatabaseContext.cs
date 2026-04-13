using AspireBoot.Domain.Entities.Users;
using AspireBoot.Infrastructure.Postgres.Mappings.Users;
using Microsoft.EntityFrameworkCore;

namespace AspireBoot.Infrastructure.Postgres;

public class DatabaseContext(DbContextOptions<DatabaseContext> dbContextOptions) : DbContext(dbContextOptions)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = new UserMapping(modelBuilder.Entity<User>());
    }
}
