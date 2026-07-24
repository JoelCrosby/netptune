using Netptune.Core.Enums;
using Netptune.Core.Events;

namespace Netptune.Automation.Common;

public interface IExecutionService
{
    IReadOnlyList<AutomationTriggerType> ScheduledTriggerTypes { get; }

    Task ExecuteEventRules<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : IEventMessage;

    Task ExecuteScheduledRules(AutomationTriggerType triggerType, CancellationToken cancellationToken);

    Task ExecuteScheduledActions(CancellationToken cancellationToken);
}
