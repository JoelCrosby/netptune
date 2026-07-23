using System.Text.Json;

using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Persistence.Actions;

internal sealed class AddCommentHandler : IActionExecutionHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IEventRecordWriter EventRecords;

    public AddCommentHandler(INetptuneUnitOfWork unitOfWork, IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        EventRecords = eventRecords;
    }

    public AutomationActionType Type => AutomationActionType.AddComment;

    public async Task<ActionOutcome> Execute(
        PlannedAutomationAction action,
        AutomationPersistenceState state,
        CancellationToken cancellationToken)
    {
        var body = action.Contribution.CommentBody;

        if (body is null)
        {
            return ActionOutcomes.InvalidContribution();
        }

        var comment = BuildComment(action, body);

        await UnitOfWork.Comments.AddRangeAsync([comment], cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        var commentEvent = BuildCommentEvent(action, comment);

        await EventRecords.Append(commentEvent, cancellationToken);

        action.Result.Output = JsonSerializer.SerializeToDocument(new
        {
            commentId = comment.Id,
        }, JsonOptions.Default);

        return ActionOutcomes.Succeeded();
    }

    private static Comment BuildComment(PlannedAutomationAction action, string body)
    {
        var execution = action.Execution;

        return new Comment
        {
            Body = body,
            EntityId = execution.Task.Id,
            EntityType = EntityType.Task,
            WorkspaceId = execution.Rule.WorkspaceId,
            OwnerId = execution.ActorUserId,
            CreatedByUserId = execution.ActorUserId,
        };
    }

    private static EventWriteRequest<CommentEventPayload> BuildCommentEvent(
        PlannedAutomationAction action,
        Comment comment)
    {
        return new EventWriteRequest<CommentEventPayload>
        {
            WorkspaceId = comment.WorkspaceId,
            EventKey = EventKeys.CommentCreated,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = comment.EntityId.ToString(),
            ActorUserId = action.Execution.ActorUserId,
            Payload = new CommentEventPayload
            {
                CommentId = comment.Id,
                RecipientUserIds = [],
            },
            References =
            [
                new EventReferenceInput
                {
                    Role = EventReferenceRoles.Member,
                    EntityType = EventEntityTypes.From(EntityType.Comment),
                    EntityId = comment.Id.ToString(),
                },
            ],
        };
    }
}
