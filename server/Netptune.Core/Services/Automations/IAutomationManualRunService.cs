using Netptune.Core.Models.Automations;

namespace Netptune.Core.Services.Automations;

public interface IAutomationManualRunService
{
    Task<AutomationManualRunResult> Execute(AutomationManualRunRequest request, CancellationToken cancellationToken = default);
}
