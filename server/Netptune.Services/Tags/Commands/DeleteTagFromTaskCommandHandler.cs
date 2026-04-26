using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tags;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Tags.Commands;

public sealed record DeleteTagFromTaskCommand(DeleteTagFromTaskRequest Request) : IRequest<ClientResponse>;

public sealed class DeleteTagFromTaskCommandHandler : IRequestHandler<DeleteTagFromTaskCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public DeleteTagFromTaskCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteTagFromTaskCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var taskId = await UnitOfWork.Tasks.GetTaskInternalId(request.Request.SystemId, workspaceKey);

        if (!taskId.HasValue) return ClientResponse.NotFound;

        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey);

        if (!workspaceId.HasValue)
        {
            return ClientResponse.Failed($"workspace with key {workspaceKey} does not exist");
        }

        var tag = await UnitOfWork.Tags.GetByValue(request.Request.Tag, workspaceId.Value);

        await UnitOfWork.Tags.DeleteTagFromTask(workspaceId.Value, taskId.Value, request.Request.Tag);
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
}
