using Netptune.Automation.Models;
using Netptune.Core.Enums;
using Netptune.Core.Events;

namespace Netptune.Automation.Common;

internal interface IAutomationRuleMatcher
{
    AutomationTriggerType TriggerType { get; }
}

internal interface IScheduledRuleMatcher : IAutomationRuleMatcher
{
    Task<List<PendingAutomationExecution>> Match(CancellationToken cancellationToken);
}

internal interface IEventRuleMatcher<TMessage> : IAutomationRuleMatcher where TMessage : IEventMessage
{
    Task<List<PendingAutomationExecution>> Match(TMessage message, CancellationToken cancellationToken);
}
