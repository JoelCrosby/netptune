using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Execution;

internal sealed class LocalAiStopSignal : IAiStopSignal
{
    private readonly List<Action<Guid>> Subscribers = [];

    public Task Publish(Guid operationId)
    {
        List<Action<Guid>> subscribers;

        lock (Subscribers)
        {
            subscribers = [.. Subscribers];
        }

        foreach (var subscriber in subscribers)
        {
            subscriber(operationId);
        }

        return Task.CompletedTask;
    }

    public void Subscribe(Action<Guid> onStop)
    {
        lock (Subscribers)
        {
            Subscribers.Add(onStop);
        }
    }
}
