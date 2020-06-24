using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories
{
    public class CommentRepository : Repository<DataContext, Comment, int>, ICommentRepository
    {
        public CommentRepository(DataContext context, IDbConnectionFactory connectionFactory)
            : base(context, connectionFactory)
        {
        }

        public Task<List<Comment>> GetCommentsForTask(int taskId, bool isReadonly = false)
        {
            return Entities
                .Where(x => !x.IsDeleted && x.EntityType == EntityType.Task && x.EntityId == taskId)
                .Include(x => x.CreatedByUser)
                .Include(x => x.Owner)
                .ApplyReadonly(isReadonly);
        }
    }
}
