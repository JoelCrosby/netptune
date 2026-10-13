using Mediator;

using Netptune.Core.Exceptions;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Handlers.Tasks.Queries;

public sealed record FindTasksByTextQuery(TaskTextSearch Search) : IRequest<ClientResponse<PagedResponse<TaskTextViewModel>>>;

public sealed class FindTasksByTextQueryHandler : IRequestHandler<FindTasksByTextQuery, ClientResponse<PagedResponse<TaskTextViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public FindTasksByTextQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<TaskTextViewModel>>> Handle(FindTasksByTextQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();

        try
        {
            var tasks = await UnitOfWork.Tasks.FindByText(workspaceId, request.Search, cancellationToken);

            return ClientResponse<PagedResponse<TaskTextViewModel>>.Success(tasks);
        }
        catch (InvalidTextPatternException exception)
        {
            return ClientResponse<PagedResponse<TaskTextViewModel>>.Failed(exception.Message);
        }
    }
}
