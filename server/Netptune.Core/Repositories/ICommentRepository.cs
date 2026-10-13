using Netptune.Core.Entities;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Comments;

namespace Netptune.Core.Repositories;

public interface ICommentRepository : IWorkspaceEntityRepository<Comment, int>
{

    Task<List<CommentViewModel>> GetCommentViewModelsForTask(int taskId, CancellationToken cancellationToken = default, PageRequest? pageRequest = null);

    Task<TaskCommentMatches> FindTaskCommentsByText(int workspaceId, IReadOnlyCollection<int> taskIds, TextPattern pattern, int take, CancellationToken cancellationToken = default);

    Task<CommentViewModel?> GetCommentViewModel(int id, CancellationToken cancellationToken = default);

    Task<CommentViewModel?> GetTaskCommentViewModel(int taskId, int commentId, CancellationToken cancellationToken = default);

    Task<Comment?> GetCommentForUpdate(int id, int workspaceId, CancellationToken cancellationToken = default);
}
