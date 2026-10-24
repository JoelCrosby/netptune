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

public sealed record CreateAutomationRuleCommand(AutomationRuleRequest Request) : IRequest<ClientResponse<AutomationRuleViewModel>>;

public sealed class CreateAutomationRuleCommandHandler
    : IRequestHandler<CreateAutomationRuleCommand, ClientResponse<AutomationRuleViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAutomationActionRegistry ActionRegistry;
    private readonly IWorkspacePermissionCache PermissionCache;
    private readonly IActivityLogger Activity;

    public CreateAutomationRuleCommandHandler(
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
        CreateAutomationRuleCommand request,
        CancellationToken cancellationToken)
    {
        var validationError = AutomationValidation.Validate(request.Request, ActionRegistry);

        if (validationError is not null)
        {
            return ClientResponse<AutomationRuleViewModel>.Failed(validationError);
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
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

        var rule = new AutomationRule
        {
            WorkspaceId = workspaceId,
            Name = ruleRequest.Name.Trim(),
            IsEnabled = ruleRequest.IsEnabled,
            ExecutionUserId = ruleRequest.ExecutionUserId,
            ProjectId = ruleRequest.ProjectId,
            BoardId = ruleRequest.BoardId,
            SprintId = ruleRequest.SprintId,
            TriggerType = ruleRequest.Trigger.Type,
            TriggerConfig = AutomationMapping.ToTriggerConfig(ruleRequest.Trigger),
            OwnerId = userId,
            CreatedByUserId = userId,
            Actions = ruleRequest.Actions.Select((action, index) => new AutomationAction
            {
                Type = action.Type,
                SortOrder = index,
                Config = AutomationMapping.ToActionConfig(action, ActionRegistry),
                OwnerId = userId,
                CreatedByUserId = userId,
            }).ToList(),
        };

        await UnitOfWork.Automations.AddAsync(rule, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.LogWith<AutomationRuleActivityMeta>(options =>
        {
            options.EntityId = rule.Id;
            options.EntityType = EntityType.AutomationRule;
            options.Type = ActivityType.Create;
            options.Meta = new AutomationRuleActivityMeta { Name = rule.Name };
        });

        var viewModel = rule.ToViewModel(ActionRegistry);

        return ClientResponse<AutomationRuleViewModel>.Success(viewModel);
    }
}
