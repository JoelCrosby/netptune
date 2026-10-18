using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;

namespace Netptune.Automation.Common;

internal static class TaskDeletionEvents
{
    public static EventWriteRequest<EntityDeletedPayload> Create(
        ProjectTask task,
        string actorUserId,
        Guid? correlationId = null,
        Guid? causationEventId = null)
    {
        return new EventWriteRequest<EntityDeletedPayload>
        {
            WorkspaceId = task.WorkspaceId,
            EventKey = EventKeys.EntityDeleted,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = task.Id.ToString(),
            ActorUserId = actorUserId,
            OriginType = EventOriginType.Automation,
            CorrelationId = correlationId,
            CausationEventId = causationEventId,
            Payload = new EntityDeletedPayload
            {
                Name = task.Name,
            },
        };
    }
}
