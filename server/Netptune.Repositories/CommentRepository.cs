using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Comments;
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
                .OrderByDescending(x => x.CreatedAt)
                .Include(x => x.CreatedByUser)
                .Include(x => x.Owner)
                .Include(x => x.Reactions)
                .ApplyReadonly(isReadonly);
        }

        public async Task<List<CommentViewModel>> GetCommentViewModelsForTask(int taskId, bool isReadonly = false)
        {
            var comments = await GetCommentsForTask(taskId, isReadonly);

            return comments.Select(comment => comment.ToViewModel()).ToList();
        }

        public async Task<CommentViewModel> GetCommentViewModel(int id, bool isReadonly = false)
        {
            var query = Entities
                .Include(x => x.CreatedByUser)
                .Include(x => x.Owner)
                .Include(x => x.Reactions);

            var comment = isReadonly
                ? await query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
                : await query.FirstOrDefaultAsync(x => x.Id == id);

            return comment.ToViewModel();
        }
    }
}
