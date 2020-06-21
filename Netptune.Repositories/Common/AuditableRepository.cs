using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;
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
    public abstract class AuditableRepository<TContext, TEntity, TId> : Repository<TContext, TEntity, TId>, IAuditableRepository<TEntity, TId>
        where TContext : DbContext
        where TEntity : AuditableEntity<TId>
    {

        protected AuditableRepository(TContext context, IDbConnectionFactory connectionFactory) : base(context, connectionFactory)
        {
        }

        public async Task<TEntity> Delete(TId id, AppUser user)
        {
            var entity = await GetAsync(id);

            if (entity is null) return null;

            entity.IsDeleted = true;
            entity.DeletedByUserId = user.Id;

            return entity;
        }

        public async Task<TEntity> DeletePermanent(TId id)
        {
            var entity = await GetAsync(id);

            if (entity is null) return null;

            Entities.Remove(entity);

            return entity;
        }

        public async Task DeletePermanent(IEnumerable<TId> idList)
        {
            var entities = await GetAllByIdAsync(idList);

            Entities.RemoveRange(entities);
        }
    }
}

