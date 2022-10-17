using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Comments;

namespace Netptune.Core.Services;

public interface ICommentService
{
    Task<ClientResponse<CommentViewModel>> AddCommentToTask(AddCommentRequest request);

    Task<List<CommentViewModel>?> GetCommentsForTask(string systemId);

    Task<ClientResponse> Delete(int id);
}
