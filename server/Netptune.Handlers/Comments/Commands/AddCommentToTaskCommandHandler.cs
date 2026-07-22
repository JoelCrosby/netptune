using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;
using Netptune.Handlers.Comments;

namespace Netptune.Handlers.Comments.Commands;

public sealed record AddCommentToTaskCommand(AddCommentRequest Request) : IRequest<ClientResponse<CommentViewModel>>;

public sealed class AddCommentToTaskCommandHandler : IRequestHandler<AddCommentToTaskCommand, ClientResponse<CommentViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IEventRecordWriter EventRecords;

    public AddCommentToTaskCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        EventRecords = eventRecords;
    }

    public async ValueTask<ClientResponse<CommentViewModel>> Handle(AddCommentToTaskCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var userId = Identity.GetCurrentUserId();
        var taskId = await UnitOfWork.Tasks.GetTaskInternalId(request.Request.SystemId, workspaceKey, cancellationToken);
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (taskId is null || !workspaceId.HasValue)
        {
            return ClientResponse<CommentViewModel>.NotFound;
        }

        var mentions = request.Request.Mentions
            .Distinct()
            .Where(id => id != userId)
            .ToList();

        var validMentionIds = mentions.Count > 0
            ? await UnitOfWork.WorkspaceUsers.GetWorkspaceUserIds(workspaceId.Value, cancellationToken)
            : [];

        var filteredMentions = mentions
            .Where(validMentionIds.Contains)
            .ToList();

        var comment = new Comment
        {
            Body = request.Request.Comment,
            EntityType = EntityType.Task,
            OwnerId = userId,
            EntityId = taskId.Value,
            WorkspaceId = workspaceId.Value,
            Mentions = filteredMentions.Select(mentionedUserId => new CommentMention
            {
                UserId = mentionedUserId,
                WorkspaceId = workspaceId.Value,
            }).ToList(),
        };

        await UnitOfWork.Comments.AddAsync(comment, cancellationToken);

        await UnitOfWork.Transaction(async () =>
        {
            await UnitOfWork.CompleteAsync(cancellationToken);

            var eventContext = new CommentEventContext(comment.WorkspaceId, comment.EntityId, comment.Id);
            var commentEvent = CommentEvents.Create(eventContext, EventKeys.CommentCreated, filteredMentions);

            await EventRecords.Append(commentEvent, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        var result = await UnitOfWork.Comments.GetCommentViewModel(comment.Id, cancellationToken);

        if (result is null)
        {
            return ClientResponse<CommentViewModel>.Failed("add comment failed");
        }

        return result;
    }
}
