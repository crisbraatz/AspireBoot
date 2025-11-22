using System.Linq.Expressions;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AspireBoot.Infrastructure.Postgres.Repositories;

public interface IBaseEntityRepository<T> where T : BaseEntity
{
    DbSet<T> DbSet { get; }
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByAsync(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default);
    void HardDeleteMany(IEnumerable<T> entities);
    void HardDeleteOne(T entity);
    Task InsertManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task InsertOneAsync(T entity, CancellationToken cancellationToken = default);

    Task<TP?> ProjectOneByAsync<TP>(
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TP>> project,
        CancellationToken cancellationToken = default) where TP : BaseListDto;

    Task<(IEnumerable<TP> data, int totalItems)> ProjectManyByAsync<TP>(
        BaseListRequestDto<T> request, Expression<Func<T, TP>> project, CancellationToken cancellationToken = default)
        where TP : BaseListDto;

    Task<IEnumerable<T>> SelectManyByAsync(
        Expression<Func<T, bool>>? filter = null, bool track = false, CancellationToken cancellationToken = default);

    Task<T?> SelectOneByAsync(
        Expression<Func<T, bool>>? filter = null, bool track = false, CancellationToken cancellationToken = default);

    void SoftDeleteMany(IEnumerable<T> entities);
    void SoftDeleteOne(T entity);
    void UpdateMany(IEnumerable<T> entities);
    void UpdateOne(T entity);
}
