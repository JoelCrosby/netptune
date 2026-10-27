namespace Netptune.Core.Services.Ai;

public interface IAiCancellationRegistry
{
    IDisposable Register(Guid operationId, CancellationTokenSource source);

    Task Stop(Guid operationId);
}
