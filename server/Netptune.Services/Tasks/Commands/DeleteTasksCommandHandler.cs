using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Tasks.Commands;

public sealed record DeleteTasksCommand(IEnumerable<int> Ids) : IRequest<ClientResponse>;

public sealed class DeleteTasksCommandHandler : IRequestHandler<DeleteTasksCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;

    public DeleteTasksCommandHandler(INetptuneUnitOfWork unitOfWork, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteTasksCommand request, CancellationToken cancellationToken)
    {
        var tasks = await UnitOfWork.Tasks.GetAllByIdAsync(request.Ids, cancellationToken: cancellationToken);
        var taskIds = tasks.ConvertAll(task => task.Id);

        var ids = await UnitOfWork.ProjectTasksInGroups.GetAllByTaskId(taskIds, cancellationToken);
        await UnitOfWork.ProjectTasksInGroups.DeletePermanent(ids, cancellationToken);

        await UnitOfWork.Tasks.DeletePermanent(taskIds, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.LogMany(options =>
        {
            options.EntityIds = taskIds;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
