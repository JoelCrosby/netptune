using Netptune.Core.Entities;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.ProjectTasks;

public sealed class TaskStatusResolver : ITaskStatusResolver
{
    private const string NewStatusKey = "new";

    private readonly INetptuneUnitOfWork UnitOfWork;

    public TaskStatusResolver(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public Task<Status?> ResolveRequested(int statusId, int workspaceId, CancellationToken cancellationToken = default)
    {
        return UnitOfWork.Statuses.GetInWorkspace(statusId, workspaceId, cancellationToken: cancellationToken);
    }

    public async Task<Status?> ResolveDefault(int? preferredStatusId, int workspaceId, CancellationToken cancellationToken = default)
    {
        if (preferredStatusId.HasValue)
        {
            var preferred = await ResolveRequested(preferredStatusId.Value, workspaceId, cancellationToken);

            if (preferred is not null)
            {
                return preferred;
            }
        }

        var newStatus = await UnitOfWork.Statuses.GetTaskStatusByKey(workspaceId, NewStatusKey, cancellationToken);

        if (newStatus is not null)
        {
            return newStatus;
        }

        return await UnitOfWork.Statuses.GetFirstTaskStatus(workspaceId, cancellationToken);
    }
}
