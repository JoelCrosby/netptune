using System;
using System.Threading.Tasks;

namespace Netptune.Core.Repositories.Common
{
    /// <summary>
    /// Container that's holds references to all repositories to provide data access to your application
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Execute all the database changes.
        /// </summary>
        /// <returns>Number of changes</returns>
        int Complete();

        /// <summary>
        /// Execute all the database changes asynchronous.
        /// </summary>
        /// <returns>Number of changes</returns>
        Task<int> CompleteAsync();

        /// <summary>
        /// Executes the passed function in a single transaction.
        /// After completion commits the changes, if it fails it rolls the changes back
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="disableChangeDetection"></param>
        Task Transaction(Func<Task> callback, bool disableChangeDetection = false);

        /// <summary>
        /// Executes the passed function in a single transaction.
        /// After completion commits the changes, if it fails it rolls the changes back
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="disableChangeDetection"></param>
        Task<TResult> Transaction<TResult>(Func<Task<TResult>> callback, bool disableChangeDetection = false);
    }
}
