using Mediator;

using Netptune.Automation.Rules;
using Netptune.Core.Enums;
using Netptune.Core.Services;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Usage;

namespace Netptune.Handlers.RelationTypes.Queries;

public sealed record GetRelationTypeUsageQuery(int Id) : IRequest<EntityUsageViewModel?>;

public sealed class GetRelationTypeUsageQueryHandler : IRequestHandler<GetRelationTypeUsageQuery, EntityUsageViewModel?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAutomationActionRegistry ActionRegistry;

    public GetRelationTypeUsageQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAutomationActionRegistry actionRegistry)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ActionRegistry = actionRegistry;
    }

    public async ValueTask<EntityUsageViewModel?> Handle(GetRelationTypeUsageQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return null;

        var relationType = await UnitOfWork.RelationTypes.GetInWorkspace(request.Id, workspaceId.Value, true, cancellationToken);

        if (relationType is null) return null;

        var relationCount = await UnitOfWork.RelationTypes.GetRelationCount(relationType.Id, cancellationToken);
        var rules = await UnitOfWork.Automations.GetRulesInWorkspace(workspaceId.Value, cancellationToken: cancellationToken);

        var subject = new AutomationReferenceSubject
        {
            Kind = UsageSubjectKind.RelationType,
            Id = relationType.Id,
            Name = relationType.Name,
        };

        var automationRules = AutomationReferences.Find(rules, ActionRegistry, subject);
        var references = UsageReferences.Build((UsageReferenceKind.AutomationRule, automationRules));
        var blockedReason = ResolveBlockedReason(relationType.IsSystem, relationCount);

        return new EntityUsageViewModel
        {
            Id = relationType.Id,
            Kind = UsageSubjectKind.RelationType,
            Name = relationType.Name,
            UsageCount = relationCount,
            References = references,
            CanDelete = blockedReason is null,
            BlockedReason = blockedReason,
        };
    }

    private static string? ResolveBlockedReason(bool isSystem, int relationCount)
    {
        if (isSystem)
        {
            return "Built-in relation types cannot be deleted.";
        }

        if (relationCount > 0)
        {
            return "Relation type is in use and cannot be deleted.";
        }

        return null;
    }
}
