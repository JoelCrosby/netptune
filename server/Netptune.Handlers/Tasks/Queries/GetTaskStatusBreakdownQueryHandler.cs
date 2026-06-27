using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Handlers.Tasks.Queries;

public sealed record GetTaskStatusBreakdownQuery : IRequest<ClientResponse<TaskStatusBreakdownViewModel>>;

public sealed class GetTaskStatusBreakdownQueryHandler : IRequestHandler<GetTaskStatusBreakdownQuery, ClientResponse<TaskStatusBreakdownViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTaskStatusBreakdownQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<TaskStatusBreakdownViewModel>> Handle(GetTaskStatusBreakdownQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var statuses = await UnitOfWork.Tasks.GetTaskStatusBreakdownAsync(workspaceKey, cancellationToken);

        var result = new TaskStatusBreakdownViewModel
        {
            Statuses = statuses,
            Total = statuses.Sum(status => status.Count),
        };

        return ClientResponse<TaskStatusBreakdownViewModel>.Success(result);
    }
}
