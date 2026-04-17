using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Comments;

namespace Netptune.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository Comments;
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public CommentService(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Comments = unitOfWork.Comments;
        Activity = activity;
    }

    public async Task<ClientResponse<CommentViewModel>> AddCommentToTask(AddCommentRequest request)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var userId = Identity.GetCurrentUserId();
        var taskId = await UnitOfWork.Tasks.GetTaskInternalId(request.SystemId, workspaceKey);
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey);

        if (taskId is null || !workspaceId.HasValue)
        {
            return ClientResponse<CommentViewModel>.NotFound;
        }

        var comment = new Comment
        {
            Body = request.Comment,
            EntityType = EntityType.Task,
            OwnerId = userId,
            EntityId = taskId.Value,
            WorkspaceId = workspaceId.Value,
        };

        await Comments.AddAsync(comment);
        await UnitOfWork.CompleteAsync();

        var result = await Comments.GetCommentViewModel(comment.Id);

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

    public async Task<List<CommentViewModel>?> GetCommentsForTask(string systemId)
    {
        var workspaceId = Identity.GetWorkspaceKey();
        var taskId = await UnitOfWork.Tasks.GetTaskInternalId(systemId, workspaceId);

        if (taskId is null)
        {
            return null;
        }

        return await Comments.GetCommentViewModelsForTask(taskId.Value);
    }

    public async Task<ClientResponse> Delete(int id)
    {
        var comment = await Comments.GetAsync(id);

        if (comment is null)
        {
            return ClientResponse.NotFound;
        }

        var commentId = comment.Id;

        await Comments.DeletePermanent(comment.Id);
        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = commentId;
            options.EntityType = EntityType.Comment;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
