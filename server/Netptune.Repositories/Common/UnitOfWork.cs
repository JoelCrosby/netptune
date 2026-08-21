using Microsoft.EntityFrameworkCore;

using Netptune.Core.Exceptions;
using Netptune.Core.Repositories.Common;

using Npgsql;

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
                throw ToTransactionException(ex);
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
                throw ToTransactionException(ex);
            }
            finally
            {
                Context.ChangeTracker.AutoDetectChangesEnabled = true;
            }
        });
    }

    private static Exception ToTransactionException(Exception exception)
    {
        var uniqueViolation = FindUniqueViolation(exception);

        if (uniqueViolation is not null)
        {
            return new UniqueConstraintException(uniqueViolation.ConstraintName, exception);
        }

        return new UnitOfWorkTransactionException(
            "UnitOfWork Transaction Failed. See Inner exception for details.", exception);
    }

    private static PostgresException? FindUniqueViolation(Exception? exception)
    {
        while (exception is not null)
        {
            if (exception is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres)
            {
                return postgres;
            }

            exception = exception.InnerException;
        }

        return null;
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
