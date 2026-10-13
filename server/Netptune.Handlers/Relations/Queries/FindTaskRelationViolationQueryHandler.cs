using Mediator;

using Netptune.Core.Relations;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Relations.Queries;

public sealed record FindTaskRelationViolationQuery(int RelationTypeId, int TaskId, int OtherTaskId) : IRequest<string?>;

public sealed class FindTaskRelationViolationQueryHandler : IRequestHandler<FindTaskRelationViolationQuery, string?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public FindTaskRelationViolationQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<string?> Handle(FindTaskRelationViolationQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return null;

        var relationType = await UnitOfWork.RelationTypes.GetInWorkspace(request.RelationTypeId, workspaceId.Value, true, cancellationToken);

        if (relationType is null) return null;

        var (source, target) = RelationTypeRules.Orient(relationType.Category, request.TaskId, request.OtherTaskId);

        return await TaskRelationRules.FindViolation(UnitOfWork, relationType, source, target, cancellationToken);
    }
}
