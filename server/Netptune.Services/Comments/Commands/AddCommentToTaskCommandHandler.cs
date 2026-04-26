using Mediator;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;

namespace Netptune.Services.Comments.Commands;

public sealed record AddCommentToTaskCommand(AddCommentRequest Request) : IRequest<ClientResponse<CommentViewModel>>;

public sealed class AddCommentToTaskCommandHandler : IRequestHandler<AddCommentToTaskCommand, ClientResponse<CommentViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public AddCommentToTaskCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<CommentViewModel>> Handle(AddCommentToTaskCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var userId = Identity.GetCurrentUserId();
        var taskId = await UnitOfWork.Tasks.GetTaskInternalId(request.Request.SystemId, workspaceKey);
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey);

        if (taskId is null || !workspaceId.HasValue)
        {
            return ClientResponse<CommentViewModel>.NotFound;
        }

        var mentions = request.Request.Mentions
            .Distinct()
            .Where(id => id != userId)
            .ToList();

        var validMentionIds = mentions.Count > 0
            ? await UnitOfWork.WorkspaceUsers.GetWorkspaceUserIds(workspaceId.Value)
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

        await UnitOfWork.Comments.AddAsync(comment);
        await UnitOfWork.CompleteAsync();

        var result = await UnitOfWork.Comments.GetCommentViewModel(comment.Id);

        if (result is null)
        {
            return ClientResponse<CommentViewModel>.Failed("add comment failed");
        }

        Activity.Log(options =>
        {
            options.EntityId = comment.Id;
            options.EntityType = EntityType.Comment;
            options.Type = ActivityType.Create;
        });

        if (filteredMentions.Count > 0)
        {
            Activity.Log(options =>
            {
                options.EntityId = comment.Id;
                options.EntityType = EntityType.Comment;
                options.Type = ActivityType.Mention;
                options.RecipientUserIds = filteredMentions;
            });
        }

        return result;
    }
}
