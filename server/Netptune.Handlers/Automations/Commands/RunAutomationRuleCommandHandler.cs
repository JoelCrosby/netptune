using Mediator;

using Netptune.Core.Events.Automations;
using Netptune.Core.Localization;
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
            return ClientResponse<AutomationManualRunViewModel>.Failed(AppText.ErrorChooseAtLeastOneTaskRun);
        }

        if (taskIds.Count > MaximumTasksPerRun)
        {
            return ClientResponse<AutomationManualRunViewModel>.Failed(
                Localized.Format(AppText.ErrorManualRunCanCoverUpTasks, MaximumTasksPerRun));
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
                AppText.ErrorSelectedTasksNotAvailableWorkspace);
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
