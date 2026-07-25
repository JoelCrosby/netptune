using Mediator;

using Netptune.Core.Events.Automations;
using Netptune.Core.Models.Automations;
using Netptune.Core.Services.Automations;

namespace Netptune.JobServer.Handlers;

public sealed class AutomationManualRunHandler : IRequestHandler<AutomationManualRunMessage>
{
    private readonly IAutomationManualRunService ManualRuns;

    public AutomationManualRunHandler(IAutomationManualRunService manualRuns)
    {
        ManualRuns = manualRuns;
    }

    public async ValueTask<Unit> Handle(AutomationManualRunMessage request, CancellationToken cancellationToken)
    {
        await ManualRuns.Execute(new AutomationManualRunRequest
        {
            RuleId = request.RuleId,
            WorkspaceId = request.WorkspaceId,
            TaskIds = request.TaskIds,
            InitiatingUserId = request.InitiatingUserId,
        }, cancellationToken);

        return Unit.Value;
    }
}
