using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Relationships;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Tasks.Commands.ReassignTasks;

public sealed class ReassignTasksCommandHandler : IRequestHandler<ReassignTasksCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;

    public ReassignTasksCommandHandler(INetptuneUnitOfWork unitOfWork, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(ReassignTasksCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var taskIdsInBoard = await UnitOfWork.Tasks.GetTaskIdsInBoard(req.BoardId);
        var taskIds = req.TaskIds.Where(id => taskIdsInBoard.Contains(id)).ToList();

        var tasks = await UnitOfWork.Tasks.GetAllByIdAsync(taskIds);

        foreach (var task in tasks)
        {
            task.UpdatedAt = DateTime.UtcNow;
            task.ProjectTaskAppUsers.Add(new ProjectTaskAppUser { UserId = req.AssigneeId });
        }

        await UnitOfWork.CompleteAsync();

        Activity.LogWithMany<AssignActivityMeta>(options =>
        {
            options.EntityIds = taskIds;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Assign;
            options.Meta = new AssignActivityMeta { AssigneeId = req.AssigneeId };
        });

        return ClientResponse.Success;
    }
}
