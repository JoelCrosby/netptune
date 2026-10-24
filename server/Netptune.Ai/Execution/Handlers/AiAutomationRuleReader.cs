using System.Text.Json;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Requests;
using Netptune.Handlers.Automations.Queries;

namespace Netptune.Ai.Execution.Handlers;

internal static class AiAutomationRuleReader
{
    public static async Task<AutomationRuleRequest?> ReadCurrent(
        IMediator mediator,
        int ruleId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAutomationRuleQuery(ruleId), cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return null;
        }

        return AiAutomationJson.ToRequest(result.Payload);
    }

    public static AutomationRuleRequest? ReadRule(JsonElement payload, string name)
    {
        var hasRule = payload.TryGetProperty(name, out var element);

        if (!hasRule)
        {
            return null;
        }

        return element.Deserialize<AutomationRuleRequest>(AiAutomationJson.Options);
    }
}
