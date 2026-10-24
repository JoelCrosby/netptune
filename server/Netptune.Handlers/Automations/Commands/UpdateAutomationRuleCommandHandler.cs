using Mediator;

using Netptune.Automation.Rules;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Handlers.Automations.Commands;

public sealed record UpdateAutomationRuleCommand(int Id, AutomationRuleRequest Request) : IRequest<ClientResponse<AutomationRuleViewModel>>;

public sealed class UpdateAutomationRuleCommandHandler
    : IRequestHandler<UpdateAutomationRuleCommand, ClientResponse<AutomationRuleViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAutomationActionRegistry ActionRegistry;
    private readonly IWorkspacePermissionCache PermissionCache;
    private readonly IActivityLogger Activity;

    public UpdateAutomationRuleCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAutomationActionRegistry actionRegistry,
        IWorkspacePermissionCache permissionCache,
        IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ActionRegistry = actionRegistry;
        PermissionCache = permissionCache;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<AutomationRuleViewModel>> Handle(
        UpdateAutomationRuleCommand request,
        CancellationToken cancellationToken)
    {
        var validationError = AutomationValidation.Validate(request.Request, ActionRegistry);

        if (validationError is not null)
        {
            return ClientResponse<AutomationRuleViewModel>.Failed(validationError);
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var rule = await UnitOfWork.Automations.GetRuleInWorkspace(request.Id, workspaceId, cancellationToken: cancellationToken);

        if (rule is null)
        {
            return ClientResponse<AutomationRuleViewModel>.NotFound;
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var ruleRequest = request.Request;
        var validationContext = new AutomationValidationContext
        {
            Request = ruleRequest,
            WorkspaceId = workspaceId,
            UserId = userId,
            WorkspaceKey = workspaceKey,
            UnitOfWork = UnitOfWork,
            PermissionCache = PermissionCache,
            ActionRegistry = ActionRegistry,
        };
        var principalError = await AutomationPrincipalValidation.Validate(validationContext, cancellationToken);

        if (principalError is not null)
        {
            return ClientResponse<AutomationRuleViewModel>.Failed(principalError);
        }

        var referenceError = await AutomationReferenceValidation.Validate(validationContext, cancellationToken);

        if (referenceError is not null)
        {
            return ClientResponse<AutomationRuleViewModel>.Failed(referenceError);
        }

        var changedSections = AutomationRuleChanges.Find(rule, ruleRequest, ActionRegistry);

        if (changedSections.Count == 0)
        {
            var unchanged = rule.ToViewModel(ActionRegistry);

            return ClientResponse<AutomationRuleViewModel>.Success(unchanged);
        }

        rule.Name = ruleRequest.Name.Trim();
        rule.IsEnabled = ruleRequest.IsEnabled;
        rule.ExecutionUserId = ruleRequest.ExecutionUserId;
        rule.ProjectId = ruleRequest.ProjectId;
        rule.BoardId = ruleRequest.BoardId;
        rule.SprintId = ruleRequest.SprintId;
        rule.TriggerType = ruleRequest.Trigger.Type;
        rule.TriggerConfig = AutomationMapping.ToTriggerConfig(ruleRequest.Trigger);
        rule.ModifiedByUserId = userId;

        var isActionsChanged = changedSections.Contains(AutomationRuleChanges.Actions);

        if (isActionsChanged)
        {
            ReplaceActions(rule, ruleRequest, userId);
        }

        var isRenameOnly = changedSections is [AutomationRuleChanges.Name];
        var cancelledCount = await UnitOfWork.Transaction(async () =>
        {
            var cancelled = isRenameOnly
                ? 0
                : await UnitOfWork.Automations.CancelPendingRuleActions(rule.Id, userId, cancellationToken);

            await UnitOfWork.CompleteAsync(cancellationToken);

            return cancelled;
        });

        Activity.LogWith<AutomationRuleActivityMeta>(options =>
        {
            options.EntityId = rule.Id;
            options.EntityType = EntityType.AutomationRule;
            options.Type = ActivityType.Modify;
            options.Meta = new AutomationRuleActivityMeta
            {
                Name = rule.Name,
                ChangedSections = changedSections,
                CancelledScheduledActions = AutomationRuleActivityMeta.CancelledCount(cancelledCount),
            };
        });

        var viewModel = rule.ToViewModel(ActionRegistry);

        return ClientResponse<AutomationRuleViewModel>.Success(viewModel);
    }

    private void ReplaceActions(AutomationRule rule, AutomationRuleRequest ruleRequest, string userId)
    {
        foreach (var existing in rule.Actions)
        {
            existing.IsDeleted = true;
            existing.DeletedByUserId = userId;
        }

        foreach (var (action, index) in ruleRequest.Actions.Select((action, index) => (action, index)))
        {
            rule.Actions.Add(new AutomationAction
            {
                Type = action.Type,
                SortOrder = index,
                Config = AutomationMapping.ToActionConfig(action, ActionRegistry),
                OwnerId = userId,
                CreatedByUserId = userId,
            });
        }
    }
}
