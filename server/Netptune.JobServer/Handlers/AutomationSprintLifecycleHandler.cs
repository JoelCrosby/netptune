using Mediator;

using Netptune.Automation.Common;
using Netptune.Core.Events.Sprints;

namespace Netptune.JobServer.Handlers;

public sealed class AutomationSprintLifecycleHandler : IRequestHandler<SprintLifecycleMessage>
{
    private readonly IExecutionService AutomationExecution;

    public AutomationSprintLifecycleHandler(IExecutionService automationExecution)
    {
        AutomationExecution = automationExecution;
    }

    public async ValueTask<Unit> Handle(SprintLifecycleMessage request, CancellationToken cancellationToken)
    {
        await AutomationExecution.ExecuteEventRules(request, cancellationToken);

        return Unit.Value;
    }
}
