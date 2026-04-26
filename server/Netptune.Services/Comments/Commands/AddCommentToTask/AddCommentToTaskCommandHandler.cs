using Mediator;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;

namespace Netptune.Services.Comments.Commands;

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

        var comment = new Comment
        {
            Body = request.Request.Comment,
            EntityType = EntityType.Task,
            OwnerId = userId,
            EntityId = taskId.Value,
            WorkspaceId = workspaceId.Value,
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

        return result;
    }
}
