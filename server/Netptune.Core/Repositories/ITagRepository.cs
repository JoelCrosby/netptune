using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Core.Repositories;

public interface ITagRepository : IWorkspaceEntityRepository<Tag, int>
{
    Task<List<Tag>> GetForTask(int taskId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<TagViewModel>> GetViewModelsForTask(int taskId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<TagViewModel>> GetViewModelsForWorkspace(int workspaceId, CancellationToken cancellationToken = default);

    Task<TagViewModel?> GetViewModel(int id, CancellationToken cancellationToken = default);

    Task<Tag?> GetByValue(string value, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<bool> Exists(string value, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<Tag>> GetTagsInWorkspace(int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<Tag>> GetTagsByValueInWorkspace(int workspaceId, IEnumerable<string> tags, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<bool> ExistsForTask(int tagId, int taskId, CancellationToken cancellationToken = default);

    Task DeleteTagFromTask(int workspaceId, int taskId, string tag, CancellationToken cancellationToken = default);
}
