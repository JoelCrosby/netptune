using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;

namespace Netptune.Handlers.Comments;

internal sealed record CommentEventContext(int WorkspaceId, int TaskId, int CommentId);

internal static class CommentEvents
{
    public static EventWriteRequest<CommentEventPayload> Create(
        CommentEventContext context,
        string eventKey,
        IReadOnlyCollection<string> recipientUserIds)
    {
        return new EventWriteRequest<CommentEventPayload>
        {
            WorkspaceId = context.WorkspaceId,
            EventKey = eventKey,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = context.TaskId.ToString(),
            Payload = new CommentEventPayload
            {
                CommentId = context.CommentId,
                RecipientUserIds = recipientUserIds,
            },
            References =
            [
                new EventReferenceInput
                {
                    Role = EventReferenceRoles.Member,
                    EntityType = EventEntityTypes.From(EntityType.Comment),
                    EntityId = context.CommentId.ToString(),
                },
            ],
        };
    }
}
