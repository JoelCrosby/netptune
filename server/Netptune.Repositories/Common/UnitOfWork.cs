using System;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Repositories.Common;

namespace Netptune.Repositories.Common;

/// <summary>
/// Base unit of work class
/// The implementation of this class should instantiate and hold reference to all repositories to provide data access to your application
/// </summary>
/// <typeparam name="TContext">Db context from entity framework</typeparam>
/// <typeparam name="TDbConnectionFactory">Dc connection factory to proper database</typeparam>
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

    /// <summary>
    /// Execute all the database changes.
    /// </summary>
    /// <returns>Number of changes</returns>
    public int Complete()
    {
        return Context.SaveChanges();
    }

    /// <summary>
    /// Execute all the database changes asynchronous.
    /// </summary>
    /// <returns>Number of changes</returns>
    public async Task<int> CompleteAsync()
    {
        return await Context.SaveChangesAsync();
    }

    /// <summary>
    /// Executes the passed function in a single transaction.
    /// After completion commits the changes, if it fails it rolls the changes back
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="disableChangeDetection"></param>
    public async Task Transaction(Func<Task> callback, bool disableChangeDetection = false)
    {
        var transaction = await Context.Database.BeginTransactionAsync();

        try
        {
            Context.ChangeTracker.AutoDetectChangesEnabled = !disableChangeDetection;

            await callback();
            // Commit transaction if all commands succeed, transaction will auto-rollback
            // when disposed if either commands fails
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
    }

    /// <summary>
    /// Executes the passed function in a single transaction.
    /// After completion commits the changes, if it fails it rolls the changes back
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="disableChangeDetection"></param>
    public async Task<TResult> Transaction<TResult>(Func<Task<TResult>> callback, bool disableChangeDetection = false)
    {
        var transaction = await Context.Database.BeginTransactionAsync();

        try
        {
            Context.ChangeTracker.AutoDetectChangesEnabled = !disableChangeDetection;

            var result = await callback();
            // Commit transaction if all commands succeed, transaction will auto-rollback
            // when disposed if either commands fails
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