using System.Linq.Expressions;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.Entities;
using AspireBoot.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AspireBoot.Infrastructure.Postgres.Repositories;

public class BaseEntityRepository<T>(DatabaseContext context) : IBaseEntityRepository<T> where T : BaseEntity
{
    public DbSet<T> Entity => context.Set<T>();

    public async Task CommitAsync(CancellationToken token = default) => await context.SaveChangesAsync(token);

    public async Task<bool> ExistsByAsync(
        Expression<Func<T, bool>>? filter = null, CancellationToken token = default) =>
        await Entity.AsNoTracking().AnyAsync(filter ?? (x => true), token);

    public void HardDeleteMany(IEnumerable<T> entities) => Entity.RemoveRange(entities);

    public void HardDeleteOne(T entity) => Entity.Remove(entity);

    public async Task InsertManyAsync(IEnumerable<T> entities, CancellationToken token = default) =>
        await Entity.AddRangeAsync(entities, token);

    public async Task InsertOneAsync(T entity, CancellationToken token = default) =>
        await Entity.AddAsync(entity, token);

    public async Task<(List<TP> data, int total)> ListByAsync<TP>(
        BaseListRequestDto<T> request, Expression<Func<T, TP>> project, CancellationToken token = default)
        where TP : BaseDataForListResponseDto
    {
        var queryable = Entity.AsQueryable();

        if (request.Filters.Count is not 0)
            queryable = request.Filters.Aggregate(queryable, (current, filter) => current.Where(filter));

        if (request.OrderBy.Any())
            foreach (var (propertyName, ascending) in request.OrderBy)
                queryable = queryable.OrderBy(propertyName, ascending);

        queryable = queryable.AsNoTracking();

        var data = await queryable.PaginateBy(request.Page, request.Size).Select(project).ToListAsync(token);

        var total = await queryable.CountAsync(token);

        return (data, total);
    }

    public async Task<IList<TP>> ProjectManyByAsync<TP>(
        Expression<Func<T, TP>> project, Expression<Func<T, bool>>? filter = null, CancellationToken token = default) =>
        await Entity.AsNoTracking().Where(filter ?? (x => true)).Select(project).ToListAsync(token);

    public async Task<TP?> ProjectOneByAsync<TP>(
        Expression<Func<T, TP>> project, Expression<Func<T, bool>>? filter = null, CancellationToken token = default) =>
        await Entity.AsNoTracking().Where(filter ?? (x => true)).Select(project).SingleOrDefaultAsync(token);

    public async Task RollbackAsync() => await context.DisposeAsync();

    public async Task<IList<T>> SelectManyByAsync(
        Expression<Func<T, bool>>? filter = null, bool track = false, CancellationToken token = default) => track
        ? await Entity.Where(filter ?? (x => true)).ToListAsync(token)
        : await Entity.AsNoTracking().Where(filter ?? (x => true)).ToListAsync(token);

    public async Task<T?> SelectOneByAsync(
        Expression<Func<T, bool>>? filter = null, bool track = false, CancellationToken token = default) => track
        ? await Entity.SingleOrDefaultAsync(filter ?? (x => true), token)
        : await Entity.AsNoTracking().SingleOrDefaultAsync(filter ?? (x => true), token);

    public void SoftDeleteMany(IEnumerable<T> entities) => UpdateMany(entities);

    public void SoftDeleteOne(T entity) => UpdateOne(entity);

    public void UpdateMany(IEnumerable<T> entities) => Entity.UpdateRange(entities);

    public void UpdateOne(T entity) => Entity.Update(entity);
}