namespace AspireBoot.Infrastructure.Postgres;

public class UnitOfWork(DatabaseContext context) : IUnitOfWork
{
    public async Task CommitAsync() => await context.SaveChangesAsync();

    public async Task RollbackAsync() => await context.DisposeAsync();
}