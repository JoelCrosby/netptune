using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Extensions;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Services
{
    public class TagService : ServiceBase<TagViewModel>, ITagService
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

        public async Task<ClientResponse<TagViewModel>> AddTag(AddTagRequest request)
        {
            var userId = await Identity.GetCurrentUserId();
            var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(request.WorkspaceSlug);

            if (!workspaceId.HasValue) return null;

            var trimmedTag = request.Tag.Trim().Capitalize();
            var existingTag = await Tags.GetByValue(trimmedTag, workspaceId.Value);

            if (existingTag is { }) return Failed("Tag Name should be unique");

            var tag = new Tag
            {
                Name = request.Tag,
                OwnerId = userId,
                WorkspaceId = workspaceId.Value,
                IsDeleted = false,
            };

            await Tags.AddAsync(tag);
            await UnitOfWork.CompleteAsync();

            return Success(tag.ToViewModel());
        }

        public async Task<ClientResponse<TagViewModel>> AddTagToTask(AddTagToTaskRequest request)
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

                var taskTag = new ProjectTaskTag {TagId = tag.Id, ProjectTaskId = taskId.Value,};

                var tagForTaskExists = await Tags.ExistsForTask(tag.Id, taskId.Value);

                if (!tagForTaskExists)
                {
                    tag.ProjectTaskTags.Add(taskTag);
                }

                await UnitOfWork.CompleteAsync();

                var response = await Tags.GetViewModel(tag.Id);

                return Success(response);
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

            var tags = await Tags.GetTagsByValueInWorkspace(workspaceId.Value, request.Tags);

            await Tags.DeletePermanent(tags);

            await UnitOfWork.CompleteAsync();

            return ClientResponse.Success();
        }

        public async Task<ClientResponse> DeleteFromTask(DeleteTagFromTaskRequest request)
        {
            var taskId = await UnitOfWork.Tasks.GetTaskInternalId(request.SystemId, request.Workspace);

            if (!taskId.HasValue) return null;

            var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(request.Workspace);

            if (!workspaceId.HasValue)
            {
                return ClientResponse.Failed($"workspace with identifier {request.Workspace} does not exist");
            }

            await UnitOfWork.Tags.DeleteTagFromTask(workspaceId.Value, taskId.Value, request.Tag);

            await UnitOfWork.CompleteAsync();

            return ClientResponse.Success();
        }

        public async Task<ClientResponse<TagViewModel>> Update(UpdateTagRequest request)
        {
            var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(request.Workspace);

            if (!workspaceId.HasValue)
            {
                return Failed($"workspace with identifier {request.Workspace} does not exist");
            }

            var tag = await UnitOfWork.Tags.GetByValue(request.CurrentValue, workspaceId.Value);

            if (tag is null) return null;

            tag.Name = request.NewValue.Trim();

            await UnitOfWork.CompleteAsync();

            return Success(tag.ToViewModel());
        }
    }
}
