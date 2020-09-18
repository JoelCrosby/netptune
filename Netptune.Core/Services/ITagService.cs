using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Core.Services
{
    public interface ITagService
    {
        Task<TagViewModel> AddTagToTask(AddTagRequest request);

        Task<List<TagViewModel>> GetTagsForTask(string systemId, string workspaceSlug);

        Task<List<TagViewModel>> GetTagsForWorkspace(string workspaceSlug);

        Task<ClientResponse> Delete(DeleteTagsRequest request);
    }
}
