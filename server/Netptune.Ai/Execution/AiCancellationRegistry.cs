using System.Collections.Concurrent;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Execution;

public sealed class AiCancellationRegistry : IAiCancellationRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> Running = new();

    public IDisposable Register(Guid operationId, CancellationTokenSource source)
    {
        Running[operationId] = source;

        return new Registration(this, operationId, source);
    }

    public bool Stop(Guid operationId)
    {
        var found = Running.TryGetValue(operationId, out var source);

        if (!found || source is null)
        {
            return false;
        }

        try
        {
            source.Cancel();

            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private void Release(Guid operationId, CancellationTokenSource source)
    {
        var current = Running.TryGetValue(operationId, out var running) ? running : null;
        var isCurrent = ReferenceEquals(current, source);

        if (isCurrent)
        {
            Running.TryRemove(operationId, out _);
        }
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
