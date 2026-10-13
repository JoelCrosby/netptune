using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
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

    public Task<TaskCommentMatches> FindTaskCommentsByText(
        int workspaceId,
        IReadOnlyCollection<int> taskIds,
        TextPattern pattern,
        int take,
        CancellationToken cancellationToken = default)
    {
        var matchesBody = TextMatch.On<Comment>(comment => comment.Body, pattern);
        var query = Entities
            .AsNoTracking()
            .Where(comment => comment.WorkspaceId == workspaceId && !comment.IsDeleted && comment.EntityType == EntityType.Task)
            .Where(comment => taskIds.Contains(comment.EntityId))
            .Where(matchesBody);

        return TextMatch.RunPatternQuery(Context, async () =>
        {
            var counts = await query
                .GroupBy(comment => comment.EntityId)
                .Select(group => new { TaskId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(group => group.TaskId, group => group.Count, cancellationToken);
            var comments = await query
                .OrderBy(comment => comment.EntityId)
                .ThenBy(comment => comment.CreatedAt)
                .ThenBy(comment => comment.Id)
                .Take(take)
                .Select(comment => new TaskCommentTextViewModel(comment.Id, comment.EntityId, comment.Body))
                .ToListAsync(cancellationToken);

            return new TaskCommentMatches(comments, counts);
        });
    }

    public Task<List<CommentViewModel>> GetCommentViewModelsForTask(int taskId, CancellationToken cancellationToken = default, PageRequest? pageRequest = null)
    {
        pageRequest ??= new PageRequest();
        var pagination = pageRequest.GetPagination();

        return Entities
            .Where(x => !x.IsDeleted && x.EntityType == EntityType.Task && x.EntityId == taskId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .AsNoTracking()
            .AsSplitQuery()
            .Select(ToViewModel())
            .ToListAsync(cancellationToken);
    }

    public Task<CommentViewModel?> GetCommentViewModel(int id, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(x => x.Id == id)
            .AsNoTracking()
            .AsSplitQuery()
            .Select(ToViewModel())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<CommentViewModel?> GetTaskCommentViewModel(int taskId, int commentId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(x => x.Id == commentId && !x.IsDeleted && x.EntityType == EntityType.Task && x.EntityId == taskId)
            .AsNoTracking()
            .AsSplitQuery()
            .Select(ToViewModel())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Comment?> GetCommentForUpdate(int id, int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Include(comment => comment.Mentions)
            .FirstOrDefaultAsync(
                comment => comment.Id == id && comment.WorkspaceId == workspaceId && !comment.IsDeleted,
                cancellationToken);
    }

    private Expression<Func<Comment, CommentViewModel>> ToViewModel()
    {
        var commentEntityType = EventEntityTypes.From(EntityType.Comment);

        return x => new CommentViewModel
        {
            Id = x.Id,
            UserDisplayName = x.Owner!.DisplayName,
            UserDisplayImage = x.Owner.PictureUrl,
            UserIsServiceAccount = x.Owner.UserType == AppUserType.ServiceAccount,
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
                DisplayName = m.User.DisplayName,
            }).ToList(),
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            IsEdited = Context.EventRecords.Any(eventRecord =>
                eventRecord.EventKey == EventKeys.CommentUpdated &&
                eventRecord.References.Any(reference =>
                    reference.EntityType == commentEntityType && reference.EntityId == x.Id.ToString())),
        };
    }
}
