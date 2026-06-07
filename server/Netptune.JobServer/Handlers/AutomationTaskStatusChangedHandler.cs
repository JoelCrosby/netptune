using Mediator;

using Netptune.Automation.Execution;
using Netptune.Core.Events.Tasks;

namespace Netptune.JobServer.Handlers;

public sealed class AutomationTaskStatusChangedHandler : IRequestHandler<TaskStatusChangedMessage>
{
    private readonly IExecutionService AutomationExecution;

    public AutomationTaskStatusChangedHandler(IExecutionService automationExecution)
    {
        AutomationExecution = automationExecution;
    }

    public async ValueTask<Unit> Handle(TaskStatusChangedMessage request, CancellationToken cancellationToken)
    {
        await AutomationExecution.ExecuteStatusChangedRules(request, cancellationToken);

        return Unit.Value;
    }
}
