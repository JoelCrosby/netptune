using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Handlers.Tasks.Queries;

public sealed record GetArchivedTasksQuery(TaskFilter? Filter = null) : IRequest<ClientResponse<PagedResponse<TaskViewModel>>>;

public sealed class GetArchivedTasksQueryHandler : IRequestHandler<GetArchivedTasksQuery, ClientResponse<PagedResponse<TaskViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetArchivedTasksQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<TaskViewModel>>> Handle(GetArchivedTasksQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var page = await UnitOfWork.Tasks.GetTasksAsync(workspaceKey, request.Filter, true, true, cancellationToken);

        return page;
    }
}
