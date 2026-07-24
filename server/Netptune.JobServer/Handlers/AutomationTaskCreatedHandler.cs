using Mediator;

using Netptune.Automation.Common;
using Netptune.Core.Events.Tasks;

namespace Netptune.JobServer.Handlers;

public sealed class AutomationTaskCreatedHandler : IRequestHandler<TaskCreatedMessage>
{
    private readonly IExecutionService AutomationExecution;

    public AutomationTaskCreatedHandler(IExecutionService automationExecution)
    {
        AutomationExecution = automationExecution;
    }

    public async ValueTask<Unit> Handle(TaskCreatedMessage request, CancellationToken cancellationToken)
    {
        await AutomationExecution.ExecuteEventRules(request, cancellationToken);

        return Unit.Value;
    }
}
