using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tags;
using Netptune.Core.Extensions;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Services;

public class TagService : ServiceBase<TagViewModel>, ITagService
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public TagService(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async Task<ClientResponse<TagViewModel>> Create(AddTagRequest request)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey);

        if (!workspaceId.HasValue)
        {
            return ClientResponse<TagViewModel>.NotFound;
        }

        var trimmedTag = request.Tag.Trim().Capitalize();
        var alreadyExists = await UnitOfWork.Tags.Exists(trimmedTag, workspaceId.Value);

        if (alreadyExists) return Failed("Tag Name should be unique");

        var tag = new Tag
        {
            Name = request.Tag,
            OwnerId = userId,
            WorkspaceId = workspaceId.Value,
            IsDeleted = false,
        };

        await UnitOfWork.Tags.AddAsync(tag);
        await UnitOfWork.CompleteAsync();

        var result = await UnitOfWork.Tags.GetViewModel(tag.Id);

        Activity.Log(options =>
        {
            options.EntityId = tag.Id;
            options.EntityType = EntityType.Tag;
            options.Type = ActivityType.Create;
        });

        return Success(result!);
    }

    public async Task<ClientResponse<TagViewModel>> AddToTask(AddTagToTaskRequest request)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceKey = Identity.GetWorkspaceKey();

        var taskId = await UnitOfWork.Tasks.GetTaskInternalId(request.SystemId, workspaceKey);
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey);

        if (taskId is null || !workspaceId.HasValue)
        {
            return ClientResponse<TagViewModel>.NotFound;
        }

        return await UnitOfWork.Transaction(async () =>
        {
            var trimmedTag = request.Tag.Trim().Capitalize();

            async Task<Tag> GetOrCreateTag()
            {
                var existingTag = await UnitOfWork.Tags.GetByValue(trimmedTag, workspaceId.Value);

                if (existingTag is {}) return existingTag;

                var entity = new Tag
                {
                    Name = request.Tag,
                    OwnerId = userId,
                    WorkspaceId = workspaceId.Value,
                    IsDeleted = false,
                };

                return await UnitOfWork.Tags.AddAsync(entity);
            }

            var tag = await GetOrCreateTag();
            var taskTag = new ProjectTaskTag {TagId = tag.Id, ProjectTaskId = taskId.Value};
            var tagForTaskExists = await UnitOfWork.Tags.ExistsForTask(tag.Id, taskId.Value);

            if (!tagForTaskExists)
            {
                tag.ProjectTaskTags.Add(taskTag);
            }

            await UnitOfWork.CompleteAsync();

            Activity.LogWith<TagActivityMeta>(options =>
            {
                options.EntityId = taskId.Value;
                options.EntityType = EntityType.Task;
                options.Type = ActivityType.AddTag;
                options.Meta = new TagActivityMeta
                {
                    TagId = tag.Id,
                    TagName = tag.Name,
                };
            });

            var response = await UnitOfWork.Tags.GetViewModel(tag.Id);

            return Success(response!);
        });
    }

    public async Task<List<TagViewModel>?> GetTagsForTask(string systemId)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var taskId = await UnitOfWork.Tasks.GetTaskInternalId(systemId, workspaceKey);

        if (taskId is null) return null;

        return await UnitOfWork.Tags.GetViewModelsForTask(taskId.Value, true);
    }

    public async Task<List<TagViewModel>?> GetTagsForWorkspace()
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey);

        if (workspaceId is null) return null;

        return await UnitOfWork.Tags.GetViewModelsForWorkspace(workspaceId.Value);
    }

    public async Task<ClientResponse> Delete(DeleteTagsRequest request)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey);

        if (workspaceId is null) return ClientResponse.Failed();

        var tags = await UnitOfWork.Tags.GetTagsByValueInWorkspace(workspaceId.Value, request.Tags);
        var tagIds = tags.ConvertAll(t => t.Id);

        await UnitOfWork.Tags.DeletePermanent(tags);
        await UnitOfWork.CompleteAsync();

        Activity.LogMany(options =>
        {
            options.EntityIds = tagIds;
            options.EntityType = EntityType.Tag;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }

    public async Task<ClientResponse> DeleteFromTask(DeleteTagFromTaskRequest request)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var taskId = await UnitOfWork.Tasks.GetTaskInternalId(request.SystemId, workspaceKey);

        if (!taskId.HasValue) return ClientResponse.NotFound;

        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey);

        if (!workspaceId.HasValue)
        {
            return ClientResponse.Failed($"workspace with key {workspaceKey} does not exist");
        }

        var tag = await UnitOfWork.Tags.GetByValue(request.Tag, workspaceId.Value);

        await UnitOfWork.Tags.DeleteTagFromTask(workspaceId.Value, taskId.Value, request.Tag);
        await UnitOfWork.CompleteAsync();

        if (tag is not null)
        {
            Activity.LogWith<TagActivityMeta>(options =>
            {
                options.EntityId = taskId.Value;
                options.EntityType = EntityType.Task;
                options.Type = ActivityType.RemoveTag;
                options.Meta = new TagActivityMeta
                {
                    TagId = tag.Id,
                    TagName = tag.Name,
                };
            });
        }

        return ClientResponse.Success;
    }

    public async Task<ClientResponse<TagViewModel>> Update(UpdateTagRequest request)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey);

        if (!workspaceId.HasValue)
        {
            return Failed($"workspace with key {workspaceKey} does not exist");
        }

        var tag = await UnitOfWork.Tags.GetByValue(request.CurrentValue, workspaceId.Value);

        if (tag is null)
        {
            return NotFound();
        }

        tag.Name = request.NewValue.Trim();

        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = tag.Id;
            options.EntityType = EntityType.Tag;
            options.Type = ActivityType.ModifyName;
        });

        var result = tag.ToViewModel();

        return Success(result);
    }
}
