using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Dapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Netptune.Core.BaseEntities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Repositories.Common;

/// <summary>
/// Base Repository compatible with entity framework core and micro ORMs like Dapper
/// Designed to do complex read queries with Dapper and write operations with EF Core
/// </summary>
/// <typeparam name="TContext"></typeparam>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TId"></typeparam>
public abstract class Repository<TContext, TEntity, TId> : ReadOnlyRepository, IRepository<TEntity, TId>
    where TContext : DbContext
    where TEntity : class, IKeyedEntity<TId>
{
    protected readonly TContext Context;
    protected readonly DbSet<TEntity> Entities;

    protected readonly string TableName;

    protected Repository(TContext context, IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
        Context = context;
        Entities = context.Set<TEntity>();

        var entityType = Context.Model.FindEntityType(typeof(TEntity));

        TableName = entityType.GetTableName();
    }

    /// <summary>
    /// Builds Equals a compilable Predicate Expression for use with EF
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    protected static Expression<Func<TEntity, bool>> EqualsPredicate(TId id)
    {
        Expression<Func<TEntity, TId>> selector = x => x.Id;
        Expression<Func<TId>> closure = () => id;

        return Expression.Lambda<Func<TEntity, bool>>(
            Expression.Equal(selector.Body, closure.Body),
            selector.Parameters);
    }

    /// <summary>
    /// Basic get query using entity id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isReadonly"></param>
    /// <returns>Entity of the defined type</returns>
    public virtual TEntity Get(TId id, bool isReadonly = false)
    {
        return Entities.IsReadonly(isReadonly).FirstOrDefault(EqualsPredicate(id));
    }

    /// <summary>
    /// Basic get query using entity id async
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isReadonly"></param>
    /// <returns>Entity of the defined type</returns>
    public virtual Task<TEntity> GetAsync(TId id, bool isReadonly = false)
    {
        return Entities.IsReadonly(isReadonly).FirstOrDefaultAsync(EqualsPredicate(id));
    }

    /// <summary>
    /// Return all Entities
    /// </summary>
    /// <param name="isReadonly"></param>
    /// <returns>List of Entities</returns>
    public virtual List<TEntity> GetAll(bool isReadonly = false)
    {
        return Entities.IsReadonly(isReadonly).ToList();
    }

    /// <summary>
    /// Return all Entities async
    /// </summary>
    /// <param name="isReadonly"></param>
    /// <returns>List of Entities</returns>
    public virtual Task<List<TEntity>> GetAllAsync(bool isReadonly = false)
    {
        return Entities.ToReadonlyListAsync(isReadonly);
    }

    /// <summary>
    /// Return all Entities from given IDs
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="isReadonly"></param>
    /// <returns>List of Entities</returns>
    public virtual List<TEntity> GetAllById(IEnumerable<TId> ids, bool isReadonly = false)
    {
        return Entities
            .Where(entity => ids.Contains(entity.Id))
            .ToReadonlyList(isReadonly);
    }

    /// <summary>
    /// Return all Entities from given IDs async
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="isReadonly"></param>
    /// <returns>List of Entities</returns>
    public virtual Task<List<TEntity>> GetAllByIdAsync(IEnumerable<TId> ids, bool isReadonly = false)
    {
        return Entities
            .Where(entity => ids.Contains(entity.Id))
            .ToReadonlyListAsync(isReadonly);
    }

    /// <summary>
    /// Return all Entities Within the given page query.
    /// </summary>
    /// <param name="pageQuery"></param>
    /// <param name="isReadonly"></param>
    /// <returns>List of Entities</returns>
    public virtual IPagedResult<TEntity> GetPagedResults(IPageQuery pageQuery, bool isReadonly = false)
    {
        return PaginateToPagedResult(Entities.IsReadonly(isReadonly), pageQuery);
    }

    /// <summary>
    /// Return all Entities Within the given page query async.
    /// </summary>
    /// <param name="pageQuery"></param>
    /// <param name="isReadonly"></param>
    /// <returns>List of Entities</returns>
    public virtual Task<IPagedResult<TEntity>> GetPagedResultsAsync(IPageQuery pageQuery, bool isReadonly = false)
    {
        return PaginateToPagedResultAsync(Entities.IsReadonly(isReadonly), pageQuery);
    }

    /// <summary>
    /// Add Entity to store
    /// </summary>
    /// <param name="entity"></param>
    /// <returns>Entity of the defined type</returns>
    public virtual TEntity Add(TEntity entity)
    {
        var entityResult = Entities.Add(entity);

        return entityResult.Entity;
    }

    /// <summary>
    /// Add Entity to store async
    /// </summary>
    /// <param name="entity"></param>
    /// <returns>Entity of the defined type</returns>
    public async virtual Task<TEntity> AddAsync(TEntity entity)
    {
        var entityResult = await Entities.AddAsync(entity);

        return entityResult.Entity;
    }

    /// <summary>
    /// Add range of Entities to store
    /// </summary>
    /// <param name="entities"></param>
    /// <returns>Entity of the defined type</returns>
    public virtual void AddRange(IEnumerable<TEntity> entities)
    {
        Entities.AddRange(entities);
    }

    /// <summary>
    /// Add range of Entities to store async
    /// </summary>
    /// <param name="entities"></param>
    /// <returns>Entity of the defined type</returns>
    public virtual Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        return Entities.AddRangeAsync(entities);
    }

    /// <summary>
    /// Paginates an IQueryable entity collection with option for orderBy predicate.
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="pageQuery"></param>
    /// <returns></returns>
    protected static IPagedResult<TEntity> PaginateToPagedResult(IQueryable<TEntity> entities, IPageQuery pageQuery)
    {
        var result = GetPagedResult(entities, pageQuery);
        var results = ApplyPagination(entities, pageQuery);

        result.Results = results.ToList();

        return result;
    }

    /// <summary>
    /// Paginates an IQueryable entity collection with option for orderBy predicate.
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="pageQuery"></param>
    /// <returns></returns>
    protected static async Task<IPagedResult<TEntity>> PaginateToPagedResultAsync(IQueryable<TEntity> entities, IPageQuery pageQuery)
    {
        var result = GetPagedResult(entities, pageQuery);
        var results = ApplyPagination(entities, pageQuery);

        result.Results = await results.ToListAsync();

        return result;
    }

    /// <summary>
    /// Paginates an IQueryable entity collection with option for orderBy predicate.
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="pageQuery"></param>
    /// <returns></returns>
    protected static IQueryable<TEntity> PaginateResults(IQueryable<TEntity> entities, IPageQuery pageQuery)
    {
        return ApplyPagination(entities, pageQuery);
    }

    /// <summary>
    /// Permanently Deletes the given entities.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public virtual async Task<TEntity> DeletePermanent(TId id)
    {
        var entity = await GetAsync(id);

        if (entity is null) return null;

        Entities.Remove(entity);

        return entity;
    }

    /// <summary>
    /// Permanently Deletes the given entities.
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public virtual async Task DeletePermanent(IEnumerable<TId> ids)
    {
        var idList = ids.ToList();

        if (idList.Count == 0)
        {
            return;
        }

        using var connection = ConnectionFactory.StartConnection();

        var transaction = Context.Database.CurrentTransaction?.GetDbTransaction();

        var idSqlString = idList
            .Aggregate(new StringBuilder(), (builder, id) => builder.AppendFormat("{0},", id))
            .ToString();

        var formatted = idSqlString[..^1];

        await connection.ExecuteAsync($"DELETE FROM {TableName} WHERE id IN ({formatted})", transaction: transaction);
    }

    /// <summary>
    /// Permanently Deletes the given entities.
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public virtual Task DeletePermanent(IEnumerable<TEntity> entities)
    {
        Entities.RemoveRange(entities);

        return Task.CompletedTask;
    }

    private static IPagedResult<TEntity> GetPagedResult(IQueryable<TEntity> entities, IPageQuery pageQuery)
    {
        return new Core.Models.Repository.PagedResult<TEntity>
        {
            PageCount = (entities.Count() + pageQuery.PageSize - 1) / pageQuery.PageSize,
            CurrentPage = pageQuery.Page,
            PageSize = pageQuery.PageSize,
            RowCount = entities.Count(),
        };
    }

    private static IQueryable<TEntity> ApplyPagination(IQueryable<TEntity> entities, IPageQuery pageQuery)
    {
        return entities
            .ApplySearchQuery(pageQuery)
            .ApplySortOrderQuery(pageQuery)
            .Skip(pageQuery.Page * pageQuery.PageSize)
            .Take(pageQuery.PageSize);
    }
}

internal static class QueryExtensions
{
    internal static IQueryable<TEntity> ApplySortOrderQuery<TEntity>(this IQueryable<TEntity> results, IPageQuery pageQuery)
    {
        if (string.IsNullOrEmpty(pageQuery.Sort)) return results;

        var order = pageQuery.SortDescending ? "desc" : "asc";

        return results.OrderBy($"{pageQuery.Sort} {order}");
    }

    internal static IQueryable<TEntity> ApplySearchQuery<TEntity>(this IQueryable<TEntity> results, IPageQuery pageQuery)
    {
        if (string.IsNullOrWhiteSpace(pageQuery.Query)) return results;

        return results.Where($"x.m == {pageQuery.Query}");
    }
}