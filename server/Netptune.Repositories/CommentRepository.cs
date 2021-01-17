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
                .ToReadonlyListAsync(isReadonly);
        }

        public async Task<List<CommentViewModel>> GetCommentViewModelsForTask(int taskId)
        {
            var comments = await GetCommentsForTask(taskId, true);

            return comments.ConvertAll(comment => comment.ToViewModel());
        }

        public async Task<CommentViewModel> GetCommentViewModel(int id)
        {
            var comment = await Entities
                .Include(x => x.CreatedByUser)
                .Include(x => x.Owner)
                .Include(x => x.Reactions)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return comment.ToViewModel();
        }
    }
}
