using Netptune.Automation.Common;
using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Automations;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Persistence.Actions;

internal sealed class FlagTaskHandler : IActionExecutionHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IEventRecordWriter EventRecords;

    public FlagTaskHandler(INetptuneUnitOfWork unitOfWork, IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        EventRecords = eventRecords;
    }

    public AutomationActionType Type => AutomationActionType.FlagTask;

    public async Task<ActionOutcome> Execute(
        PlannedAutomationAction action,
        AutomationPersistenceState state,
        CancellationToken cancellationToken)
    {
        if (action.Contribution.Flag is null)
        {
            return ActionOutcomes.InvalidContribution();
        }

        if (!state.Flags.TryGetValue(action, out var flag))
        {
            return new ActionOutcome(
                AutomationActionResultStatus.Skipped,
                AutomationMessage.TaskAlreadyFlagged);
        }

        await UnitOfWork.Flags.AddRangeAsync([flag], cancellationToken);
        await EventRecords.Append(BuildFlagEvent(action, flag), cancellationToken);

        return ActionOutcomes.Succeeded();
    }

    private static EventWriteRequest<FlagRaisedPayload> BuildFlagEvent(PlannedAutomationAction action, Flag flag)
    {
        var execution = action.Execution;
        var task = execution.Task;

        return new EventWriteRequest<FlagRaisedPayload>
        {
            WorkspaceId = task.WorkspaceId,
            EventKey = EventKeys.FlagRaised,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = task.Id.ToString(),
            ActorUserId = execution.ExecutionUserId!,
            OriginType = EventOriginType.Automation,
            CorrelationId = execution.CorrelationId,
            CausationEventId = execution.CausationEventId,
            Payload = new FlagRaisedPayload
            {
                FlagName = flag.Name,
                AutomationRuleId = flag.AutomationRuleId,
            },
        };
    }
}
