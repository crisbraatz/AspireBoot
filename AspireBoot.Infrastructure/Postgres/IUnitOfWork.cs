namespace AspireBoot.Infrastructure.Postgres;

public interface IUnitOfWork
{
    Task CommitAsync();
    Task RollbackAsync();
}
