using Mediator;

using Netptune.Core.Events.Automations;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Handlers.Automations.Commands;

public sealed record RunAutomationRuleCommand(int Id, AutomationManualRunRequestBody Request)
    : IRequest<ClientResponse<AutomationManualRunViewModel>>;

public sealed class RunAutomationRuleCommandHandler
    : IRequestHandler<RunAutomationRuleCommand, ClientResponse<AutomationManualRunViewModel>>
{
    private const int MaximumTasksPerRun = 50;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IEventPublisher EventPublisher;
    private readonly IIdentityService Identity;

    public RunAutomationRuleCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        EventPublisher = eventPublisher;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<AutomationManualRunViewModel>> Handle(
        RunAutomationRuleCommand request,
        CancellationToken cancellationToken)
    {
        var taskIds = request.Request.TaskIds.Distinct().ToList();

        if (taskIds.Count == 0)
        {
            return ClientResponse<AutomationManualRunViewModel>.Failed("Choose at least one task to run.");
        }

        if (taskIds.Count > MaximumTasksPerRun)
        {
            return ClientResponse<AutomationManualRunViewModel>.Failed(
                $"A manual run can cover up to {MaximumTasksPerRun} tasks.");
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var rule = await UnitOfWork.Automations.GetRuleInWorkspace(
            request.Id,
            workspaceId,
            true,
            cancellationToken);

        if (rule is null)
        {
            return ClientResponse<AutomationManualRunViewModel>.NotFound;
        }

        var validTaskIds = await UnitOfWork.Tasks.GetValidTaskIdsInWorkspace(taskIds, workspaceId, cancellationToken);

        if (validTaskIds.Count == 0)
        {
            return ClientResponse<AutomationManualRunViewModel>.Failed(
                "The selected tasks are not available in this workspace.");
        }

        await EventPublisher.Dispatch(new AutomationManualRunMessage
        {
            WorkspaceId = workspaceId,
            RuleId = rule.Id,
            TaskIds = validTaskIds,
            InitiatingUserId = Identity.GetCurrentUserId(),
        });

        var view = new AutomationManualRunViewModel
        {
            RuleId = rule.Id,
            TaskCount = validTaskIds.Count,
        };

        return ClientResponse<AutomationManualRunViewModel>.Success(view);
    }
}
