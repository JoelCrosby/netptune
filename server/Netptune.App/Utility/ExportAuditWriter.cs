using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.App.Utility;

public sealed record ExportAuditDetails(string ExportType, string? Scope = null);

public static class ExportAuditWriter
{
    public static async Task LogExportRequested(
        IEventRecordWriter eventRecords,
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        ExportAuditDetails details,
        CancellationToken cancellationToken)
    {
        var workspaceId = await identity.GetWorkspaceId();

        await eventRecords.Append(new EventWriteRequest<ExportRequestedPayload>
        {
            WorkspaceId = workspaceId,
            EventKey = EventKeys.ExportRequested,
            SubjectType = EventEntityTypes.From(EntityType.Workspace),
            SubjectId = workspaceId.ToString(),
            Payload = new ExportRequestedPayload
            {
                ExportType = details.ExportType,
                Scope = details.Scope,
            },
        }, cancellationToken);

        await unitOfWork.CompleteAsync(cancellationToken);
    }
}
