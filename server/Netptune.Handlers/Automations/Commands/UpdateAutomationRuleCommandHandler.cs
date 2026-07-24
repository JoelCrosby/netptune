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

public sealed record UpdateAutomationRuleCommand(int Id, AutomationRuleRequest Request) : IRequest<ClientResponse<AutomationRuleViewModel>>;

public sealed class UpdateAutomationRuleCommandHandler
    : IRequestHandler<UpdateAutomationRuleCommand, ClientResponse<AutomationRuleViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAutomationActionRegistry ActionRegistry;
    private readonly IWorkspacePermissionCache PermissionCache;

    public UpdateAutomationRuleCommandHandler(
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

        rule.Name = ruleRequest.Name.Trim();
        rule.IsEnabled = ruleRequest.IsEnabled;
        rule.ExecutionUserId = ruleRequest.ExecutionUserId;
        rule.TriggerType = ruleRequest.Trigger.Type;
        rule.TriggerConfig = AutomationMapping.ToTriggerConfig(ruleRequest.Trigger);
        rule.ModifiedByUserId = userId;

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

        await UnitOfWork.CompleteAsync(cancellationToken);

        var viewModel = rule.ToViewModel(ActionRegistry);

        return ClientResponse<AutomationRuleViewModel>.Success(viewModel);
    }
}
