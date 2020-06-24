using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Requests;

namespace Netptune.Core.Services
{
    public interface ICommentService
    {
        Task<Comment> AddCommentToTask(AddCommentRequest request);

        Task<List<Comment>> GetCommentsForTask(string systemId, string workspaceSlug);
    }
}
