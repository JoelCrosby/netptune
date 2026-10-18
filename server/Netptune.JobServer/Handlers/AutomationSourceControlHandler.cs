using Mediator;

using Netptune.Automation.Common;
using Netptune.Core.Events.SourceControl;

namespace Netptune.JobServer.Handlers;

public sealed class AutomationSourceControlHandler : IRequestHandler<SourceControlTaskEventMessage>
{
    private readonly IExecutionService AutomationExecution;

    public AutomationSourceControlHandler(IExecutionService automationExecution)
    {
        AutomationExecution = automationExecution;
    }

    public async ValueTask<Unit> Handle(SourceControlTaskEventMessage request, CancellationToken cancellationToken)
    {
        await AutomationExecution.ExecuteEventRules(request, cancellationToken);

        return Unit.Value;
    }
}
