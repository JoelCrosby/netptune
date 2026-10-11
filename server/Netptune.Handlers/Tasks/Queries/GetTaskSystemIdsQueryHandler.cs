using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tasks.Queries;

public sealed record GetTaskSystemIdsQuery(IReadOnlyCollection<int> TaskIds) : IRequest<Dictionary<int, string>>;

public sealed class GetTaskSystemIdsQueryHandler : IRequestHandler<GetTaskSystemIdsQuery, Dictionary<int, string>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTaskSystemIdsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<Dictionary<int, string>> Handle(
        GetTaskSystemIdsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.TaskIds.Count == 0)
        {
            return [];
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var references = await UnitOfWork.Tasks.GetTaskSearchReferences(request.TaskIds, workspaceKey, cancellationToken);

        return references.ToDictionary(reference => reference.TaskId, reference => reference.SystemId);
    }
}
