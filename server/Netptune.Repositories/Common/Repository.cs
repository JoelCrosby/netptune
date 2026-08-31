using System.Linq.Expressions;
using System.Text;

using Dapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Netptune.Core.BaseEntities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Repositories.Common;

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

        if (entityType is null)
        {
            throw new($"could not find EntityType for type {typeof(TEntity).FullName}");
        }

        TableName = entityType.GetTableName()!;
    }

    protected static Expression<Func<TEntity, bool>> EqualsPredicate(TId id)
    {
        Expression<Func<TEntity, TId>> selector = x => x.Id;
        Expression<Func<TId>> closure = () => id;

        return Expression.Lambda<Func<TEntity, bool>>(
            Expression.Equal(selector.Body, closure.Body),
            selector.Parameters);
    }

    public virtual Task<TEntity?> GetAsync(TId id, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities.IsReadonly(isReadonly).FirstOrDefaultAsync(EqualsPredicate(id), cancellationToken);
    }

    public virtual Task<List<TEntity>> GetAllByIdAsync(IEnumerable<TId> ids, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(entity => ids.Contains(entity.Id))
            .ToReadonlyListAsync(isReadonly, cancellationToken);
    }

    public async virtual Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var entityResult = await Entities.AddAsync(entity, cancellationToken);

        return entityResult.Entity;
    }

    public virtual Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        return Entities.AddRangeAsync(entities, cancellationToken);
    }

    public virtual async Task<TEntity?> DeletePermanent(TId id, CancellationToken cancellationToken = default)
    {
        var entity = await GetAsync(id, cancellationToken: cancellationToken);

        if (entity is null) return null;

        Entities.Remove(entity);

        return entity;
    }

    public virtual async Task DeletePermanent(IEnumerable<TId> ids, CancellationToken cancellationToken = default)
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

        await connection.ExecuteAsync(new CommandDefinition($"DELETE FROM {TableName} WHERE id IN ({formatted})", transaction: transaction, cancellationToken: cancellationToken));
    }

    public virtual Task DeletePermanent(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        Entities.RemoveRange(entities);

        return Task.CompletedTask;
    }
}
