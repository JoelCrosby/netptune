using System.Collections.Concurrent;

using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Execution;

public sealed class AiTurnRegistry : IAiTurnRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> Running = new();

    public IDisposable Register(Guid conversationId, CancellationTokenSource source)
    {
        Running[conversationId] = source;

        return new Registration(this, conversationId, source);
    }

    public bool Stop(Guid conversationId)
    {
        var found = Running.TryGetValue(conversationId, out var source);

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

    private void Release(Guid conversationId, CancellationTokenSource source)
    {
        var current = Running.TryGetValue(conversationId, out var running) ? running : null;
        var isCurrent = ReferenceEquals(current, source);

        if (isCurrent)
        {
            Running.TryRemove(conversationId, out _);
        }
    }

    private sealed class Registration : IDisposable
    {
        private readonly AiTurnRegistry Registry;
        private readonly Guid ConversationId;
        private readonly CancellationTokenSource Source;

        public Registration(AiTurnRegistry registry, Guid conversationId, CancellationTokenSource source)
        {
            Registry = registry;
            ConversationId = conversationId;
            Source = source;
        }

        public void Dispose()
        {
            Registry.Release(ConversationId, Source);
        }
    }
}
