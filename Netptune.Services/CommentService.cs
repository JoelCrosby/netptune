using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository Comments;
        private readonly INetptuneUnitOfWork UnitOfWork;
        private readonly IIdentityService Identity;

        public CommentService(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
        {
            UnitOfWork = unitOfWork;
            Identity = identity;
            Comments = unitOfWork.Comments;
        }

        public async Task<Comment> AddCommentToTask(AddCommentRequest request)
        {
            var userId = await Identity.GetCurrentUserId();
            var taskId = await UnitOfWork.Tasks.GetTaskInternalId(request.SystemId, request.WorkspaceSlug);

            var comment = new Comment
            {
                Body = request.Comment,
                EntityType = EntityType.Task,
                OwnerId = userId,
                EntityId = taskId,
            };

            await Comments.AddAsync(comment);

            await UnitOfWork.CompleteAsync();

            return comment;
        }

        public async Task<List<Comment>> GetCommentsForTask(string systemId, string workspaceSlug)
        {
            var taskId = await UnitOfWork.Tasks.GetTaskInternalId(systemId, workspaceSlug);

            return await Comments.GetCommentsForTask(taskId, true);
        }
    }
}
