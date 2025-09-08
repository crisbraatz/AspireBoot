using AspireBoot.Domain.Entities.Users;
using AspireBoot.Infrastructure.Postgres.Mappings;
using Microsoft.EntityFrameworkCore;

namespace AspireBoot.Infrastructure.Postgres;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        _ = new UserMapping(builder.Entity<User>());
    }
}