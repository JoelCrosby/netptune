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
    public class TagRepository : Repository<DataContext, Tag, int>, ITagRepository
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
                select tag).ToListAsync();
        }

        public Task<List<TagViewModel>> GetViewModelsForTask(int taskId, bool isReadonly = false)
        {
            return (from tag in Entities
                join ptt in Context.ProjectTaskTags on tag.Id equals ptt.TagId
                join task in Context.ProjectTasks on ptt.ProjectTaskId equals task.Id
                where !task.IsDeleted && !tag.IsDeleted && task.Id == taskId
                select tag.ToViewModel()).ToListAsync();
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
                OwnerId = result.OwnerId
            };
        }

        public Task<Tag> GetByValue(string value, int workspaceId)
        {
            return Entities
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.WorkspaceId == workspaceId && x.Name == value);
        }

        public Task<bool> ExistsForTask(int tagId, int taskId)
        {
            return Context.ProjectTaskTags.AnyAsync(x => x.TagId == tagId && x.ProjectTaskId == taskId);
        }
    }
}
