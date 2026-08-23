using Mediator;

using Netptune.Automation.Rules;
using Netptune.Core.Enums;
using Netptune.Core.Services;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Usage;

namespace Netptune.Handlers.Tags.Queries;

public sealed record GetTagUsageQuery(int Id) : IRequest<EntityUsageViewModel?>;

public sealed class GetTagUsageQueryHandler : IRequestHandler<GetTagUsageQuery, EntityUsageViewModel?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAutomationActionRegistry ActionRegistry;

    public GetTagUsageQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAutomationActionRegistry actionRegistry)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ActionRegistry = actionRegistry;
    }

    public async ValueTask<EntityUsageViewModel?> Handle(GetTagUsageQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return null;

        var tag = await UnitOfWork.Tags.GetAsync(request.Id, true, cancellationToken);
        var isInWorkspace = tag is not null && tag.WorkspaceId == workspaceId.Value && !tag.IsDeleted;

        if (!isInWorkspace) return null;

        var taskCount = await UnitOfWork.Tags.GetTaskCount(tag!.Id, cancellationToken);
        var rules = await UnitOfWork.Automations.GetRulesInWorkspace(workspaceId.Value, cancellationToken: cancellationToken);

        var subject = new AutomationReferenceSubject
        {
            Kind = UsageSubjectKind.Tag,
            Id = tag.Id,
            Name = tag.Name,
        };

        var automationRules = AutomationReferences.Find(rules, ActionRegistry, subject);
        var references = UsageReferences.Build((UsageReferenceKind.AutomationRule, automationRules));

        return new EntityUsageViewModel
        {
            Id = tag.Id,
            Kind = UsageSubjectKind.Tag,
            Name = tag.Name,
            UsageCount = taskCount,
            References = references,
            CanDelete = true,
        };
    }
}
