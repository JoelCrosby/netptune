using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tasks.Queries;

public sealed record GetTaskIdsBySystemIdQuery(IReadOnlyCollection<string> SystemIds, bool IncludeArchived = false)
    : IRequest<Dictionary<string, int>>;

public sealed class GetTaskIdsBySystemIdQueryHandler : IRequestHandler<GetTaskIdsBySystemIdQuery, Dictionary<string, int>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTaskIdsBySystemIdQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<Dictionary<string, int>> Handle(GetTaskIdsBySystemIdQuery request, CancellationToken cancellationToken)
    {
        if (request.SystemIds.Count == 0)
        {
            return [];
        }

        var workspaceKey = Identity.GetWorkspaceKey();

        return await UnitOfWork.Tasks.GetTaskIdsBySystemIds(
            request.SystemIds,
            workspaceKey,
            request.IncludeArchived,
            cancellationToken);
    }
}
