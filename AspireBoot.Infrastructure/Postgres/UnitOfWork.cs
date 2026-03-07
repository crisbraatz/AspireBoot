namespace AspireBoot.Infrastructure.Postgres;

public class UnitOfWork(DatabaseContext databaseContext) : IUnitOfWork
{
    public async Task CommitAsync() => await databaseContext.SaveChangesAsync().ConfigureAwait(false);

    public async Task RollbackAsync() => await databaseContext.DisposeAsync().ConfigureAwait(false);
}
