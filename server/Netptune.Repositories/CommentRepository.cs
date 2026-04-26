using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Comments;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class CommentRepository : WorkspaceEntityRepository<DataContext, Comment, int>, ICommentRepository
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
            .Include(x => x.Owner)
            .Include(x => x.Reactions)
            .Include(x => x.Mentions).ThenInclude(m => m.User)
            .ToReadonlyListAsync(isReadonly);
    }

    public Task<List<CommentViewModel>> GetCommentViewModelsForTask(int taskId)
    {
        return Entities
            .Where(x => !x.IsDeleted && x.EntityType == EntityType.Task && x.EntityId == taskId)
            .OrderByDescending(x => x.CreatedAt)
            .AsNoTracking()
            .Select(ToViewModel())
            .ToListAsync();
    }

    public Task<CommentViewModel?> GetCommentViewModel(int id)
    {
        return Entities
            .Where(x => x.Id == id)
            .AsNoTracking()
            .Select(ToViewModel())
            .FirstOrDefaultAsync();
    }

    private static Expression<Func<Comment, CommentViewModel>> ToViewModel()
    {
        return x => new CommentViewModel
        {
            Id = x.Id,
            UserDisplayName = string.IsNullOrEmpty(x.Owner!.Firstname) && string.IsNullOrEmpty(x.Owner.Lastname)
                ? x.Owner.UserName!
                : x.Owner.Firstname + " " + x.Owner.Lastname,
            UserDisplayImage = x.Owner.PictureUrl,
            UserId = x.OwnerId!,
            Body = x.Body,
            EntityId = x.EntityId,
            EntityType = x.EntityType,
            Reactions = x.Reactions.Select(r => new ReactionViewModel
            {
                Value = r.Value,
                UserId = r.OwnerId ?? r.CreatedByUserId!,
            }).ToList(),
            Mentions = x.Mentions.Select(m => new CommentMentionViewModel
            {
                UserId = m.UserId,
                DisplayName = string.IsNullOrEmpty(m.User.Firstname) && string.IsNullOrEmpty(m.User.Lastname)
                    ? m.User.UserName!
                    : m.User.Firstname + " " + m.User.Lastname,
            }).ToList(),
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
        };
    }
}
