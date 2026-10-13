using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Handlers.Tasks.Queries;

public sealed record GetTaskTextsQuery(int? ProjectId, PageRequest Page) : IRequest<ClientResponse<PagedResponse<TaskTextViewModel>>>;

public sealed class GetTaskTextsQueryHandler : IRequestHandler<GetTaskTextsQuery, ClientResponse<PagedResponse<TaskTextViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTaskTextsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<TaskTextViewModel>>> Handle(GetTaskTextsQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var texts = await UnitOfWork.Tasks.GetTexts(workspaceId, request.ProjectId, request.Page, cancellationToken);

        return ClientResponse<PagedResponse<TaskTextViewModel>>.Success(texts);
    }
}
