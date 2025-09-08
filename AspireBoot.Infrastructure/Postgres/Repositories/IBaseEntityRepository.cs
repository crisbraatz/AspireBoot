using System.Linq.Expressions;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AspireBoot.Infrastructure.Postgres.Repositories;

public interface IBaseEntityRepository<T> where T : BaseEntity
{
    DbSet<T> Entity { get; }

    Task Commit(CancellationToken token = default);
    Task<bool> ExistsByAsync(Expression<Func<T, bool>>? filter = null, CancellationToken token = default);
    void HardDeleteMany(IEnumerable<T> entities);
    void HardDeleteOne(T entity);
    Task InsertManyAsync(IEnumerable<T> entities, CancellationToken token = default);
    Task InsertOneAsync(T entity, CancellationToken token = default);

    Task<(List<TP> data, int total)> ListByAsync<TP>(
        BaseListRequestDto<T> request, Expression<Func<T, TP>> project, CancellationToken token = default)
        where TP : BaseDataForListResponseDto;

    Task<IList<TP>> ProjectManyByAsync<TP>(
        Expression<Func<T, TP>> project, Expression<Func<T, bool>>? filter = null, CancellationToken token = default);

    Task<TP?> ProjectOneByAsync<TP>(
        Expression<Func<T, TP>> project, Expression<Func<T, bool>>? filter = null, CancellationToken token = default);

    Task<IList<T>> SelectManyByAsync(
        Expression<Func<T, bool>>? filter = null, bool track = false, CancellationToken token = default);

    Task<T?> SelectOneByAsync(
        Expression<Func<T, bool>>? filter = null, bool track = false, CancellationToken token = default);

    void SoftDeleteMany(IEnumerable<T> entities);
    void SoftDeleteOne(T entity);
    void UpdateMany(IEnumerable<T> entities);
    void UpdateOne(T entity);
}