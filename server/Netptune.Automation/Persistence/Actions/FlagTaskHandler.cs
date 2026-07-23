using Netptune.Automation.Models;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Persistence.Actions;

internal sealed class FlagTaskHandler : IActionExecutionHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public FlagTaskHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
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
                "The task already has a flag from this rule.");
        }

        await UnitOfWork.Flags.AddRangeAsync([flag], cancellationToken);

        return ActionOutcomes.Succeeded();
    }
}
