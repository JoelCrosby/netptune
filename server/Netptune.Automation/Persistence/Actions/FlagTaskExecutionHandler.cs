using Netptune.Automation.Models;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Persistence.Actions;

internal sealed class FlagTaskExecutionHandler : IAutomationActionExecutionHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public FlagTaskExecutionHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public AutomationActionType Type => AutomationActionType.FlagTask;

    public async Task<AutomationActionExecutionOutcome> Execute(
        PlannedAutomationAction action,
        AutomationPersistenceState state,
        CancellationToken cancellationToken)
    {
        if (action.Contribution.Flag is null)
        {
            return AutomationActionExecutionOutcomes.InvalidContribution();
        }

        if (!state.Flags.TryGetValue(action, out var flag))
        {
            return new AutomationActionExecutionOutcome(
                AutomationActionResultStatus.Skipped,
                "The task already has a flag from this rule.");
        }

        await UnitOfWork.Flags.AddRangeAsync([flag], cancellationToken);

        return AutomationActionExecutionOutcomes.Succeeded();
    }
}
