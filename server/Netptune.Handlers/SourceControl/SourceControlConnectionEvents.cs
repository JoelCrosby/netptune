using Netptune.Core.Entities;
using Netptune.Core.Events;

namespace Netptune.Handlers.SourceControl;

internal static class SourceControlConnectionEvents
{
    // Not an EntityType, so connection changes reach the audit log without projecting an entry
    // into the workspace activity feed.
    private const string SubjectType = "source-control-connection";

    public static EventWriteRequest<SourceControlConnectionEventPayload> Create(
        SourceControlConnection connection,
        string eventKey)
    {
        return new EventWriteRequest<SourceControlConnectionEventPayload>
        {
            WorkspaceId = connection.WorkspaceId,
            EventKey = eventKey,
            SubjectType = SubjectType,
            SubjectId = connection.Id.ToString(),
            Payload = new SourceControlConnectionEventPayload
            {
                ConnectionId = connection.Id,
                Provider = connection.Provider.ToString(),
                AccountLogin = connection.AccountLogin,
                ExternalAccountId = connection.ExternalAccountId,
            },
        };
    }
}
