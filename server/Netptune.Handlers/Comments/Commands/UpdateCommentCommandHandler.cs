using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Events;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;
using Netptune.Handlers.Comments;

namespace Netptune.Handlers.Comments.Commands;

public sealed record UpdateCommentCommand(int Id, UpdateCommentRequest Request) : IRequest<ClientResponse<CommentViewModel>>;

public sealed class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, ClientResponse<CommentViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IEventRecordWriter EventRecords;

    public UpdateCommentCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        EventRecords = eventRecords;
    }

    public async ValueTask<ClientResponse<CommentViewModel>> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var comment = await UnitOfWork.Comments.GetCommentForUpdate(request.Id, workspaceId, cancellationToken);

        if (comment is null)
        {
            return ClientResponse<CommentViewModel>.NotFound;
        }

        var userId = Identity.GetCurrentUserId();

        if (comment.OwnerId != userId)
        {
            return ClientResponse<CommentViewModel>.Forbidden;
        }

        var body = request.Request.Comment.Trim();

        if (string.IsNullOrWhiteSpace(body))
        {
            return ClientResponse<CommentViewModel>.Failed("Comment cannot be empty.");
        }

        var requestedMentionIds = request.Request.Mentions
            .Distinct()
            .Where(mentionedUserId => mentionedUserId != userId)
            .ToList();

        var workspaceUserIds = requestedMentionIds.Count > 0
            ? await UnitOfWork.WorkspaceUsers.GetWorkspaceUserIds(comment.WorkspaceId, cancellationToken)
            : [];

        var mentionIds = requestedMentionIds
            .Where(workspaceUserIds.Contains)
            .ToList();

        var existingMentions = comment.Mentions
            .ToDictionary(mention => mention.UserId);

        var newMentionIds = mentionIds
            .Where(mentionedUserId => !existingMentions.ContainsKey(mentionedUserId))
            .ToList();

        comment.Body = body;

        foreach (var mention in comment.Mentions.Where(mention => !mentionIds.Contains(mention.UserId)).ToList())
        {
            comment.Mentions.Remove(mention);
        }

        foreach (var mentionedUserId in newMentionIds)
        {
            comment.Mentions.Add(new CommentMention
            {
                UserId = mentionedUserId,
                WorkspaceId = comment.WorkspaceId,
            });
        }

        var eventContext = new CommentEventContext(comment.WorkspaceId, comment.EntityId, comment.Id);
        var commentEvent = CommentEvents.Create(eventContext, EventKeys.CommentUpdated, newMentionIds);

        await EventRecords.Append(commentEvent, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        var result = await UnitOfWork.Comments.GetCommentViewModel(comment.Id, cancellationToken);

        if (result is null)
        {
            return ClientResponse<CommentViewModel>.Failed("update comment failed");
        }

        return result;
    }
}
