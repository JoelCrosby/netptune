using Mediator;

using Netptune.Automation.Execution;
using Netptune.Core.Events.Tasks;

namespace Netptune.JobServer.Handlers;

public sealed class AutomationTaskChangedHandler : IRequestHandler<TaskChangedMessage>
{
    private readonly IExecutionService AutomationExecution;

    public AutomationTaskChangedHandler(IExecutionService automationExecution)
    {
        AutomationExecution = automationExecution;
    }

    public async ValueTask<Unit> Handle(TaskChangedMessage request, CancellationToken cancellationToken)
    {
        await AutomationExecution.ExecuteTaskChangedRules(request, cancellationToken);

        return Unit.Value;
    }
}
