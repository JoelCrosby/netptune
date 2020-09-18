using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Core.Repositories
{
    public interface ITagRepository : IRepository<Tag, int>
    {
        Task<List<Tag>> GetForTask(int taskId, bool isReadonly = false);

        Task<List<TagViewModel>> GetViewModelsForTask(int taskId, bool isReadonly = false);

        Task<List<TagViewModel>> GetViewModelsForWorkspace(int workspaceId);

        Task<TagViewModel> GetViewModel(int id);

        Task<Tag> GetByValue(string value, int workspaceId);

        Task<List<Tag>> GetTagsInWorkspace(int workspaceId, IEnumerable<string> tags);

        Task<bool> ExistsForTask(int tagId, int taskId);
    }
}
