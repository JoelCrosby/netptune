using Mediator;

using Netptune.Automation.Common;
using Netptune.Core.Events.Relations;

namespace Netptune.JobServer.Handlers;

public sealed class AutomationTaskRelationChangedHandler : IRequestHandler<TaskRelationChangedMessage>
{
    private readonly IExecutionService AutomationExecution;

    public AutomationTaskRelationChangedHandler(IExecutionService automationExecution)
    {
        AutomationExecution = automationExecution;
    }

    public async ValueTask<Unit> Handle(TaskRelationChangedMessage request, CancellationToken cancellationToken)
    {
        await AutomationExecution.ExecuteEventRules(request, cancellationToken);

        return Unit.Value;
    }
}
