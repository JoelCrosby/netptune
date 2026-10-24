using Mediator;

using Netptune.Automation.Rules;
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
using Netptune.Handlers.Automations.Queries;

namespace Netptune.Handlers.Automations.Commands;

public sealed record CreateAutomationRuleCommand(AutomationRuleRequest Request) : IRequest<ClientResponse<AutomationRuleViewModel>>;

public sealed class CreateAutomationRuleCommandHandler
    : IRequestHandler<CreateAutomationRuleCommand, ClientResponse<AutomationRuleViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAutomationActionRegistry ActionRegistry;
    private readonly IActivityLogger Activity;
    private readonly IMediator Mediator;

    public CreateAutomationRuleCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAutomationActionRegistry actionRegistry,
        IActivityLogger activity,
        IMediator mediator)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ActionRegistry = actionRegistry;
        Activity = activity;
        Mediator = mediator;
    }

    public async ValueTask<ClientResponse<AutomationRuleViewModel>> Handle(
        CreateAutomationRuleCommand request,
        CancellationToken cancellationToken)
    {
        var ruleRequest = request.Request;
        var validation = await Mediator.Send(new ValidateAutomationRuleQuery(ruleRequest), cancellationToken);

        if (!validation.IsSuccess)
        {
            return ClientResponse<AutomationRuleViewModel>.Failed(validation.Message);
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
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
