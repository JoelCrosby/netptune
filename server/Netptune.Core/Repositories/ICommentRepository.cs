using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Comments;

namespace Netptune.Core.Repositories;

public interface ICommentRepository : IWorkspaceEntityRepository<Comment, int>
{
    Task<List<Comment>> GetCommentsForTask(int taskId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<CommentViewModel>> GetCommentViewModelsForTask(int taskId, CancellationToken cancellationToken = default);

    Task<CommentViewModel?> GetCommentViewModel(int id, CancellationToken cancellationToken = default);
}
