using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Handlers.Users.Queries;

public sealed record GetAssigneesQuery(AssigneeFilter Filter)
    : IRequest<ClientResponse<PagedResponse<AssigneeViewModel>>>;

public sealed class GetAssigneesQueryHandler
    : IRequestHandler<GetAssigneesQuery, ClientResponse<PagedResponse<AssigneeViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAssigneesQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<AssigneeViewModel>>> Handle(
        GetAssigneesQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var result = await UnitOfWork.Users.GetWorkspaceAssigneesPaged(
            workspaceId,
            request.Filter,
            cancellationToken);

        var response = new PagedResponse<AssigneeViewModel>(
            [.. result.Results],
            result.CurrentPage,
            result.PageSize,
            result.RowCount);

        return ClientResponse<PagedResponse<AssigneeViewModel>>.Success(response);
    }
}
