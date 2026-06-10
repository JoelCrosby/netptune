using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Handlers.Tasks.Queries;

public sealed record GetTasksQuery(TaskFilter? Filter = null) : IRequest<ClientResponse<PagedResponse<TaskViewModel>>>;

public sealed class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, ClientResponse<PagedResponse<TaskViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTasksQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<TaskViewModel>>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var page = await UnitOfWork.Tasks.GetTasksAsync(workspaceKey, request.Filter, true, cancellationToken);

        return page;
    }
}
