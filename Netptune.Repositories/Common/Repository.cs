using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

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
        where TEntity : class
    {
        protected readonly TContext Context;
        protected readonly DbSet<TEntity> Entities;

        protected Repository(TContext context, IDbConnectionFactory connectionFactory) : base(connectionFactory)
        {
            Context = context;
            Entities = context.Set<TEntity>();
        }

        /// <summary>
        /// Basic get query using entity id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Entity of the defined type</returns>
        public virtual TEntity Get(TId id)
        {
            return Entities.Find(id);
        }

        /// <summary>
        /// Basic get query using entity id async
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Entity of the defined type</returns>
        public virtual async Task<TEntity> GetAsync(TId id)
        {
            return await Entities.FindAsync(id);
        }

        /// <summary>
        /// Return all Entities
        /// </summary>
        /// <returns>List of Entities</returns>
        public virtual IList<TEntity> GetAll()
        {
            return Entities.ToList();
        }

        /// <summary>
        /// Return all Entities async
        /// </summary>
        /// <returns>List of Entities</returns>
        public virtual async Task<IList<TEntity>> GetAllAsync()
        {
            return await Entities.ToListAsync();
        }

        /// <summary>
        /// Return all Entities Within the given page query.
        /// </summary>
        /// <returns>List of Entities</returns>
        public virtual IPagedResult<TEntity> GetPagedResults(IPageQuery pageQuery)
        {
            return PaginateToPagedResult(Entities, pageQuery);
        }

        /// <summary>
        /// Return all Entities Within the given page query async.
        /// </summary>
        /// <returns>List of Entities</returns>
        public virtual async Task<IPagedResult<TEntity>> GetPagedResultsAsync(IPageQuery pageQuery)
        {
            return await PaginateToPagedResultAsync(Entities, pageQuery);
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

        private static IPagedResult<TEntity> GetPagedResult(IQueryable<TEntity> entities, IPageQuery pagequery)
        {
            return new Core.Models.Repository.PagedResult<TEntity>
            {
                PageCount = (entities.Count() + pagequery.PageSize - 1) / pagequery.PageSize,
                CurrentPage = pagequery.Page,
                PageSize = pagequery.PageSize,
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
