using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Handlers.Users.Queries;

public sealed record GetAssigneesByIdQuery(IReadOnlyCollection<string> UserIds) : IRequest<List<AssigneeViewModel>>;

public sealed class GetAssigneesByIdQueryHandler : IRequestHandler<GetAssigneesByIdQuery, List<AssigneeViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAssigneesByIdQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<AssigneeViewModel>> Handle(
        GetAssigneesByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserIds.Count == 0)
        {
            return [];
        }

        var workspaceId = await Identity.GetWorkspaceId();

        return await UnitOfWork.Users.GetWorkspaceAssignees(workspaceId, request.UserIds, cancellationToken);
    }
}
