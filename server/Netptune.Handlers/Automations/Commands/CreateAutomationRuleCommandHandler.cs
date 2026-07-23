using Mediator;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
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

    public CreateAutomationRuleCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAutomationActionRegistry actionRegistry,
        IWorkspacePermissionCache permissionCache)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ActionRegistry = actionRegistry;
        PermissionCache = permissionCache;
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
        var req = request.Request;
        var principalError = await AutomationPrincipalValidation.Validate(
            req,
            workspaceId,
            userId,
            Identity.GetWorkspaceKey(),
            UnitOfWork,
            PermissionCache,
            ActionRegistry,
            cancellationToken);

        if (principalError is not null)
        {
            return ClientResponse<AutomationRuleViewModel>.Failed(principalError);
        }

        var rule = new AutomationRule
        {
            WorkspaceId = workspaceId,
            Name = req.Name.Trim(),
            IsEnabled = req.IsEnabled,
            ExecutionUserId = req.ExecutionUserId,
            TriggerType = req.Trigger.Type,
            TriggerConfig = AutomationMapping.ToTriggerConfig(req.Trigger),
            OwnerId = userId,
            CreatedByUserId = userId,
            Actions = req.Actions.Select((action, index) => new AutomationAction
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

        var viewModel = rule.ToViewModel(ActionRegistry);

        return ClientResponse<AutomationRuleViewModel>.Success(viewModel);
    }
}
