using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories
{
    public interface ICommentRepository : IRepository<Comment, int>
    {
        Task<List<Comment>> GetCommentsForTask(int taskId, bool isReadonly = false);
    }
}
