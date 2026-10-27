using System.Collections.Concurrent;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Execution;

public sealed class AiCancellationRegistry : IAiCancellationRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> Running = new();
    private readonly IAiStopSignal Signal;

    public AiCancellationRegistry(IAiStopSignal signal)
    {
        Signal = signal;
        Signal.Subscribe(CancelLocal);
    }

    public IDisposable Register(Guid operationId, CancellationTokenSource source)
    {
        Running[operationId] = source;

        return new Registration(this, operationId, source);
    }

    public Task Stop(Guid operationId)
    {
        CancelLocal(operationId);

        return Signal.Publish(operationId);
    }

    private void CancelLocal(Guid operationId)
    {
        var found = Running.TryGetValue(operationId, out var source);

        if (!found || source is null)
        {
            return;
        }

        try
        {
            source.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void Release(Guid operationId, CancellationTokenSource source)
    {
        Running.TryRemove(new KeyValuePair<Guid, CancellationTokenSource>(operationId, source));
    }

    private sealed class Registration : IDisposable
    {
        private readonly AiCancellationRegistry Registry;
        private readonly Guid OperationId;
        private readonly CancellationTokenSource Source;

        public Registration(AiCancellationRegistry registry, Guid operationId, CancellationTokenSource source)
        {
            Registry = registry;
            OperationId = operationId;
            Source = source;
        }

        public void Dispose()
        {
            Registry.Release(OperationId, Source);
        }
    }
}
