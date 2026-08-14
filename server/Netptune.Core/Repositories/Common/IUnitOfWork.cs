namespace Netptune.Core.Repositories.Common;

public interface IUnitOfWork
{
    Task<int> CompleteAsync(CancellationToken cancellationToken = default);

    Task Transaction(Func<Task> callback, bool disableChangeDetection = false);

    Task<TResult> Transaction<TResult>(Func<Task<TResult>> callback, bool disableChangeDetection = false);
}
