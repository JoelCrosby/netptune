namespace Netptune.Core.Repositories.Common;

public interface IAdvisoryLock
{
    // A postgres session lock belongs to the connection that took it, so the returned handle owns a
    // dedicated connection and holds the lock until it is disposed. Returns null when another holder has it.
    Task<IAsyncDisposable?> TryAcquire(long key, CancellationToken cancellationToken = default);
}
