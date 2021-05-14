using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Tags;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories
{
    public class TagRepository : WorkspaceEntityRepository<DataContext, Tag, int>, ITagRepository
    {
        public TagRepository(DataContext context, IDbConnectionFactory connectionFactory)
            : base(context, connectionFactory)
        {
        }

        public Task<List<Tag>> GetForTask(int taskId, bool isReadonly = false)
        {
            return (from tag in Entities
                join ptt in Context.ProjectTaskTags on tag.Id equals ptt.TagId
                join task in Context.ProjectTasks on ptt.ProjectTaskId equals task.Id
                where !task.IsDeleted && !tag.IsDeleted && task.Id == taskId
                select tag)
                .IsReadonly(isReadonly)
                .ToListAsync();
        }

        public Task<List<TagViewModel>> GetViewModelsForTask(int taskId, bool isReadonly = false)
        {
            return (from tag in Entities
                join ptt in Context.ProjectTaskTags on tag.Id equals ptt.TagId
                join task in Context.ProjectTasks on ptt.ProjectTaskId equals task.Id
                join owner in Context.Users on tag.OwnerId equals owner.Id
                where !task.IsDeleted && !tag.IsDeleted && task.Id == taskId
                select new
                {
                    tag.Id,
                    tag.Name,
                    OwnerId = owner.Id,
                    OwnerFirstname = owner.Firstname,
                    OwnerLastname = owner.Lastname,
                })
                .Select(t => new TagViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    OwnerId = t.OwnerId,
                    OwnerName = $"{t.OwnerFirstname} {t.OwnerLastname}",
                })
                .IsReadonly(isReadonly)
                .ToListAsync();
        }

        public async Task<TagViewModel> GetViewModel(int id)
        {
            var result = await Entities
                .Include(tag => tag.Owner)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (result is null) return null;

            return new TagViewModel
            {
                Id = id,
                Name = result.Name,
                OwnerName = result.Owner.DisplayName,
                OwnerId = result.OwnerId,
            };
        }

        public Task<bool> Exists(string value, int workspaceId)
        {
            var trimmed = value.Trim();

            return Entities
                .AnyAsync(x => !x.IsDeleted && x.WorkspaceId == workspaceId && x.Name == trimmed);
        }

        public Task<List<TagViewModel>> GetViewModelsForWorkspace(int workspaceId)
        {
            return Entities
                .Include(tag => tag.Owner)
                .Where(tag => !tag.IsDeleted && tag.WorkspaceId == workspaceId)
                .OrderBy(x => x.CreatedAt)
                .Select(tag => tag.ToViewModel())
                .ToListAsync();
        }

        public Task<Tag> GetByValue(string value, int workspaceId, bool isReadonly = false)
        {
            var trimmed = value.Trim();

            return Entities
                .IsReadonly(isReadonly)
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.WorkspaceId == workspaceId && x.Name == trimmed);
        }

        public Task<List<Tag>> GetTagsInWorkspace(int workspaceId, bool isReadonly = false)
        {
            return Entities
                .Where(x => !x.IsDeleted && x.WorkspaceId == workspaceId)
                .OrderBy(x => x.CreatedAt)
                .IsReadonly(isReadonly)
                .ToListAsync();
        }

        public Task<List<Tag>> GetTagsByValueInWorkspace(int workspaceId, IEnumerable<string> tags, bool isReadonly = false)
        {
            var tagsList = tags.ToList();

            return Entities
                .Where(x => !x.IsDeleted && x.WorkspaceId == workspaceId && tagsList.Contains(x.Name))
                .IsReadonly(isReadonly)
                .ToListAsync();
        }

        public Task<bool> ExistsForTask(int tagId, int taskId)
        {
            return Context.ProjectTaskTags.AnyAsync(x => x.TagId == tagId && x.ProjectTaskId == taskId);
        }

        public override async Task DeletePermanent(IEnumerable<Tag> entities)
        {
            var entityList = entities.ToList();
            var entityIds = entityList.Select(x => x.Id);
            var taskTags = await Context.ProjectTaskTags.Where(x => entityIds.Contains(x.TagId)).ToListAsync();

            Context.ProjectTaskTags.RemoveRange(taskTags);
            Entities.RemoveRange(entityList);
        }

        public async Task DeleteTagFromTask(int workspaceId, int taskId, string tag)
        {
            var tagTrimmed = tag.Trim();
            var tagIds = await Entities
                .Where(x => x.Name == tagTrimmed && x.WorkspaceId == workspaceId)
                .Select(x => x.Id)
                .ToListAsync();

            var taskTags = await Context.ProjectTaskTags
                .Where(x => x.ProjectTaskId == taskId && tagIds.Contains(x.TagId))
                .ToListAsync();

            Context.ProjectTaskTags.RemoveRange(taskTags);
        }
    }
}
