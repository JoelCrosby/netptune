using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Models.Usage;
using Netptune.Core.Services;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Usage;
using Netptune.Handlers.Automations;
using Netptune.Handlers.Usage;

namespace Netptune.Handlers.Statuses.Queries;

public sealed record GetStatusUsageQuery(int Id) : IRequest<EntityUsageViewModel?>;

public sealed class GetStatusUsageQueryHandler : IRequestHandler<GetStatusUsageQuery, EntityUsageViewModel?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAutomationActionRegistry ActionRegistry;

    public GetStatusUsageQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAutomationActionRegistry actionRegistry)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ActionRegistry = actionRegistry;
    }

    public async ValueTask<EntityUsageViewModel?> Handle(GetStatusUsageQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return null;

        var status = await UnitOfWork.Statuses.GetInWorkspace(request.Id, workspaceId.Value, true, cancellationToken);

        if (status is null) return null;

        var usage = await UnitOfWork.Statuses.GetUsage(status.Id, workspaceId.Value, cancellationToken);
        var rules = await UnitOfWork.Automations.GetRulesInWorkspace(workspaceId.Value, cancellationToken: cancellationToken);

        var subject = new AutomationReferenceSubject
        {
            Kind = UsageSubjectKind.Status,
            Id = status.Id,
            Name = status.Name,
        };

        var automationRules = AutomationReferences.Find(rules, ActionRegistry, subject);

        var references = UsageReferences.Build(
            (UsageReferenceKind.Project, usage.Projects),
            (UsageReferenceKind.BoardGroup, usage.BoardGroups),
            (UsageReferenceKind.AutomationRule, automationRules));

        var blockedReason = ResolveBlockedReason(status.IsSystem, usage);

        return new EntityUsageViewModel
        {
            Id = status.Id,
            Kind = UsageSubjectKind.Status,
            Name = status.Name,
            UsageCount = usage.TaskCount,
            References = references,
            CanDelete = blockedReason is null,
            BlockedReason = blockedReason,
        };
    }

    private static string? ResolveBlockedReason(bool isSystem, StatusUsage usage)
    {
        if (isSystem)
        {
            return "System statuses cannot be deleted.";
        }

        var hasTasks = usage.TaskCount > 0;
        var hasProjects = usage.Projects.Count > 0;
        var isInUse = hasTasks || hasProjects;

        if (isInUse)
        {
            return "Status is in use and cannot be deleted.";
        }

        return null;
    }
}
