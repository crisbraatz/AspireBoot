using System.Linq.Expressions;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.Entities;
using AspireBoot.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AspireBoot.Infrastructure.Postgres.Repositories;

public class BaseEntityRepository<T>(DatabaseContext databaseContext) : IBaseEntityRepository<T> where T : BaseEntity
{
    public DbSet<T> DbSet => databaseContext.Set<T>();

    public async Task CommitAsync(CancellationToken cancellationToken = default) =>
        await databaseContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    public async Task<bool> ExistsByAsync(
        Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().AnyAsync(filter ?? (x => true), cancellationToken).ConfigureAwait(false);

    public void HardDeleteMany(IEnumerable<T> entities) => DbSet.RemoveRange(entities);

    public void HardDeleteOne(T entity) => DbSet.Remove(entity);

    public async Task InsertManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) =>
        await DbSet.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);

    public async Task InsertOneAsync(T entity, CancellationToken cancellationToken = default) =>
        await DbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);

    public async Task<TP?> ProjectOneByAsync<TP>(
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TP>> project,
        CancellationToken cancellationToken = default) where TP : BaseListDto =>
        await DbSet
            .AsNoTracking()
            .Where(filter ?? (_ => true))
            .Select(project)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<(IEnumerable<TP> data, int totalItems)> ProjectManyByAsync<TP>(
        BaseListRequestDto<T> request, Expression<Func<T, TP>> project, CancellationToken cancellationToken = default)
        where TP : BaseListDto
    {
        IQueryable<T> queryable = DbSet.AsQueryable();

        if (request.Filters.Count is not 0)
            queryable = request.Filters.Aggregate(queryable, (current, filter) => current.Where(filter));

        if (request.OrderBy.Any())
            foreach ((string propertyName, bool ascending) in request.OrderBy)
                queryable = queryable.OrderBy(propertyName, ascending);

        queryable = queryable.AsNoTracking();

        IEnumerable<TP> data = await queryable
            .PaginateBy(request.CurrentPage, request.Size)
            .Select(project)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        int totalItems = await queryable.CountAsync(cancellationToken).ConfigureAwait(false);

        return (data, totalItems);
    }

    public async Task<IEnumerable<T>> SelectManyByAsync(
        Expression<Func<T, bool>>? filter = null, bool track = false, CancellationToken cancellationToken = default) =>
        track
            ? await DbSet
                .Where(filter ?? (x => true))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : await DbSet
                .AsNoTracking()
                .Where(filter ?? (x => true))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

    public async Task<T?> SelectOneByAsync(
        Expression<Func<T, bool>>? filter = null, bool track = false, CancellationToken cancellationToken = default) =>
        track
            ? await DbSet
                .SingleOrDefaultAsync(filter ?? (x => true), cancellationToken)
                .ConfigureAwait(false)
            : await DbSet
                .AsNoTracking()
                .SingleOrDefaultAsync(filter ?? (x => true), cancellationToken)
                .ConfigureAwait(false);

    public void SoftDeleteMany(IEnumerable<T> entities) => UpdateMany(entities);

    public void SoftDeleteOne(T entity) => UpdateOne(entity);

    public void UpdateMany(IEnumerable<T> entities) => DbSet.UpdateRange(entities);

    public void UpdateOne(T entity) => DbSet.Update(entity);
}
