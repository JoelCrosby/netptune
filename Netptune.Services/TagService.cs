using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Extensions;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Services
{
    public class TagService : ITagService
    {
        private readonly INetptuneUnitOfWork UnitOfWork;
        private readonly IIdentityService Identity;
        private readonly ITagRepository Tags;

        public TagService(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
        {
            UnitOfWork = unitOfWork;
            Identity = identity;
            Tags = unitOfWork.Tags;
        }

        public async Task<TagViewModel> AddTagToTask(AddTagRequest request)
        {
            var userId = await Identity.GetCurrentUserId();
            var taskId = await UnitOfWork.Tasks.GetTaskInternalId(request.SystemId, request.WorkspaceSlug);
            var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(request.WorkspaceSlug);

            if (taskId is null || !workspaceId.HasValue) return null;

            return await UnitOfWork.Transaction(async () =>
            {
                var trimmedTag = request.Tag.Trim().Capitalize();

                async Task<Tag> GetOrCreateTag()
                {
                    var existingTag = await Tags.GetByValue(trimmedTag, workspaceId.Value);

                    if (existingTag is {}) return existingTag;

                    var entity = new Tag
                    {
                        Name = request.Tag,
                        OwnerId = userId,
                        WorkspaceId = workspaceId.Value,
                        IsDeleted = false,
                    };

                    return await Tags.AddAsync(entity);
                }

                var tag = await GetOrCreateTag();

                var taskTag = new ProjectTaskTag
                {
                    TagId = tag.Id,
                    ProjectTaskId = taskId.Value,
                };

                var tagForTaskExists = await Tags.ExistsForTask(tag.Id, taskId.Value);

                if (!tagForTaskExists)
                {
                    tag.ProjectTaskTags.Add(taskTag);
                }

                await UnitOfWork.CompleteAsync();

                return await Tags.GetViewModel(tag.Id);
            });
        }

        public async Task<List<TagViewModel>> GetTagsForTask(string systemId, string workspaceSlug)
        {
            var taskId = await UnitOfWork.Tasks.GetTaskInternalId(systemId, workspaceSlug);

            if (taskId is null) return null;

            return await Tags.GetViewModelsForTask(taskId.Value, true);
        }

        public async Task<List<TagViewModel>> GetTagsForWorkspace(string workspaceSlug)
        {
            var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceSlug);

            if (workspaceId is null) return null;

            return await Tags.GetViewModelsForWorkspace(workspaceId.Value);
        }

        public async Task<ClientResponse> Delete(DeleteTagsRequest request)
        {
            var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(request.Workspace);

            if (workspaceId is null) return ClientResponse.Failed();

            var tags = await Tags.GetTagsInWorkspace(workspaceId.Value, request.Tags);

            await Tags.DeletePermanent(tags);

            return ClientResponse.Success();
        }
    }
}
