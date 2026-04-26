using Mediator;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tags;
using Netptune.Core.Extensions;
using Netptune.Core.Relationships;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Services.Tags.Commands;

public sealed class AddTagToTaskCommandHandler : IRequestHandler<AddTagToTaskCommand, ClientResponse<TagViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public AddTagToTaskCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<TagViewModel>> Handle(AddTagToTaskCommand request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceKey = Identity.GetWorkspaceKey();

        var taskId = await UnitOfWork.Tasks.GetTaskInternalId(request.Request.SystemId, workspaceKey);
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey);

        if (taskId is null || !workspaceId.HasValue)
        {
            return ClientResponse<TagViewModel>.NotFound;
        }

        return await UnitOfWork.Transaction(async () =>
        {
            var trimmedTag = request.Request.Tag.Trim().Capitalize();

            async Task<Tag> GetOrCreateTag()
            {
                var existingTag = await UnitOfWork.Tags.GetByValue(trimmedTag, workspaceId.Value);

                if (existingTag is not null) return existingTag;

                var entity = new Tag
                {
                    Name = request.Request.Tag,
                    OwnerId = userId,
                    WorkspaceId = workspaceId.Value,
                    IsDeleted = false,
                };

                return await UnitOfWork.Tags.AddAsync(entity);
            }

            var tag = await GetOrCreateTag();
            var taskTag = new ProjectTaskTag { TagId = tag.Id, ProjectTaskId = taskId.Value };
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

            return ClientResponse<TagViewModel>.Success(response!);
        });
    }
}
