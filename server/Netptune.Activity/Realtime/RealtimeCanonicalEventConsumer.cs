using Netptune.Core.Events;
using Netptune.Core.Services.Realtime;

namespace Netptune.Activity.Realtime;

public sealed class RealtimeCanonicalEventConsumer : ICanonicalEventConsumer
{
    private readonly IWorkspaceRefreshQueue RefreshQueue;

    public RealtimeCanonicalEventConsumer(IWorkspaceRefreshQueue refreshQueue)
    {
        RefreshQueue = refreshQueue;
    }

    public static string DurableName => "netptune-realtime-canonical-v1";

    // A refresh for a change made before the consumer existed tells no open client anything.
    public static bool ReplaysHistory => false;

    public ValueTask Handle(CanonicalEventEnvelope envelope, CancellationToken cancellationToken)
    {
        var scopes = BackgroundChangeScopes.For(envelope);

        if (envelope.WorkspaceId is { } workspaceId && scopes.Count > 0)
        {
            RefreshQueue.Enqueue(workspaceId, scopes);
        }

        return ValueTask.CompletedTask;
    }
}
