using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Comments;

namespace Netptune.Core.Repositories;

public interface ICommentRepository : IWorkspaceEntityRepository<Comment, int>
{
    Task<List<Comment>> GetCommentsForTask(int taskId, bool isReadonly = false);

    Task<List<CommentViewModel>> GetCommentViewModelsForTask(int taskId);

    Task<CommentViewModel?> GetCommentViewModel(int id);
}
