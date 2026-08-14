using Microsoft.EntityFrameworkCore;

using Netptune.Core.Repositories.Common;

namespace Netptune.Repositories.Common;

public abstract class UnitOfWork<TContext, TDbConnectionFactory> : IUnitOfWork
    where TContext : DbContext
    where TDbConnectionFactory : IDbConnectionFactory
{
    protected readonly TContext Context;
    protected readonly TDbConnectionFactory ConnectionFactory;

    protected UnitOfWork(TContext context, TDbConnectionFactory connectionFactory)
    {
        Context = context;
        ConnectionFactory = connectionFactory;
    }

    public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task Transaction(Func<Task> callback, bool disableChangeDetection = false)
    {
        // The retrying execution strategy rejects a transaction it did not open itself. The callback
        // still runs at most once — UnitOfWorkTransactionException is not retryable.
        var strategy = Context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Context.Database.BeginTransactionAsync();

            try
            {
                Context.ChangeTracker.AutoDetectChangesEnabled = !disableChangeDetection;

                await callback();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                throw new UnitOfWorkTransactionException(
                    "UnitOfWork Transaction Failed. See Inner exception for details.", ex);
            }
            finally
            {
                Context.ChangeTracker.AutoDetectChangesEnabled = true;
            }
        });
    }

    public async Task<TResult> Transaction<TResult>(Func<Task<TResult>> callback, bool disableChangeDetection = false)
    {
        var strategy = Context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Context.Database.BeginTransactionAsync();

            try
            {
                Context.ChangeTracker.AutoDetectChangesEnabled = !disableChangeDetection;

                var result = await callback();

                await transaction.CommitAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw new UnitOfWorkTransactionException("UnitOfWork Transaction Failed. See Inner exception for details.", ex);
            }
            finally
            {
                Context.ChangeTracker.AutoDetectChangesEnabled = true;
            }
        });
    }
}

public class UnitOfWorkTransactionException : Exception
{
    public UnitOfWorkTransactionException()
    {
    }

    public UnitOfWorkTransactionException(string message)
        : base(message)
    {
    }

    public UnitOfWorkTransactionException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
