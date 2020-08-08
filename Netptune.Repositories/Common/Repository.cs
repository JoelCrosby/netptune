using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.BaseEntities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Repositories.Common
{
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

        protected Repository(TContext context, IDbConnectionFactory connectionFactory) : base(connectionFactory)
        {
            Context = context;
            Entities = context.Set<TEntity>();
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
            return Entities.IsReadonly(isReadonly).ToListAsync();
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
                .IsReadonly(isReadonly)
                .ToList();
        }

        /// <summary>
        /// Return all Entities from given IDs async
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="isReadonly"></param>
        /// <returns>List of Entities</returns>
        public Task<List<TEntity>> GetAllByIdAsync(IEnumerable<TId> ids, bool isReadonly = false)
        {
            return Entities
                .Where(entity => ids.Contains(entity.Id))
                .IsReadonly(isReadonly)
                .ToListAsync();
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
        public TEntity Add(TEntity entity)
        {
            var entityResult = Entities.Add(entity);

            return entityResult.Entity;
        }

        /// <summary>
        /// Add Entity to store async
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Entity of the defined type</returns>
        public async Task<TEntity> AddAsync(TEntity entity)
        {
            var entityResult = await Entities.AddAsync(entity);

            return entityResult.Entity;
        }

        /// <summary>
        /// Add range of Entities to store
        /// </summary>
        /// <param name="entities"></param>
        /// <returns>Entity of the defined type</returns>
        public void AddRange(IEnumerable<TEntity> entities)
        {
            Entities.AddRange(entities);
        }

        /// <summary>
        /// Add range of Entities to store async
        /// </summary>
        /// <param name="entities"></param>
        /// <returns>Entity of the defined type</returns>
        public Task AddRangeAsync(IEnumerable<TEntity> entities)
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

        private static IPagedResult<TEntity> GetPagedResult(IQueryable<TEntity> entities, IPageQuery pageQuery)
        {
            return new Core.Models.Repository.PagedResult<TEntity>
            {
                PageCount = (entities.Count() + pageQuery.PageSize - 1) / pageQuery.PageSize,
                CurrentPage = pageQuery.Page,
                PageSize = pageQuery.PageSize,
                RowCount = entities.Count()
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
}
