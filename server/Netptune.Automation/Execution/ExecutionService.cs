using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Common;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Matching;
using Netptune.Core.Enums;
using Netptune.Core.Events;

namespace Netptune.Automation.Execution;

internal sealed class ExecutionService : IExecutionService
{
    private readonly AutomationTriggerRegistry TriggerRegistry;
    private readonly RuleExecutor RuleExecutor;
    private readonly ScheduledActionService ScheduledActions;
    private readonly ILogger<ExecutionService> Logger;

    public IReadOnlyList<AutomationTriggerType> ScheduledTriggerTypes => TriggerRegistry.ScheduledTriggerTypes;

    public ExecutionService(
        AutomationTriggerRegistry triggerRegistry,
        RuleExecutor ruleExecutor,
        ScheduledActionService scheduledActions,
        ILogger<ExecutionService> logger)
    {
        TriggerRegistry = triggerRegistry;
        RuleExecutor = ruleExecutor;
        ScheduledActions = scheduledActions;
        Logger = logger;
    }

    public async Task ExecuteEventRules<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : IEventMessage
    {
        var matchers = TriggerRegistry.GetEventMatchers<TMessage>();

        foreach (var matcher in matchers)
        {
            await ExecuteEventMatcher(matcher, message, cancellationToken);
        }
    }

    private async Task ExecuteEventMatcher<TMessage>(
        IEventRuleMatcher<TMessage> matcher,
        TMessage message,
        CancellationToken cancellationToken)
        where TMessage : IEventMessage
    {
        var triggerType = matcher.TriggerType;

        using var activity = Telemetry.StartActivity("automation.execute_event_rules", triggerType);
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            var executions = await matcher.Match(message, cancellationToken);

            await RuleExecutor.Execute(triggerType, executions, cancellationToken);
        }
        catch (Exception ex)
        {
            Telemetry.MarkFailed(activity, ex);
            Logger.LogError(ex, "{TriggerType} automation execution failed", triggerType);

            throw;
        }
        finally
        {
            Telemetry.RecordExecutionDuration(triggerType, Stopwatch.GetElapsedTime(startedAt));
        }
    }

    public async Task ExecuteScheduledRules(AutomationTriggerType triggerType, CancellationToken cancellationToken)
    {
        var matcher = TriggerRegistry.GetScheduledMatcher(triggerType);

        using var activity = Telemetry.StartActivity("automation.execute_scheduled_rules", triggerType);
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            var executions = await matcher.Match(cancellationToken);

            await RuleExecutor.Execute(triggerType, executions, cancellationToken);
        }
        catch (Exception ex)
        {
            Telemetry.MarkFailed(activity, ex);
            Logger.LogError(ex, "Scheduled {TriggerType} automation execution failed", triggerType);

            throw;
        }
        finally
        {
            Telemetry.RecordExecutionDuration(triggerType, Stopwatch.GetElapsedTime(startedAt));
        }
    }

    public Task ExecuteScheduledActions(CancellationToken cancellationToken)
    {
        return ScheduledActions.ExecuteDue(cancellationToken);
    }
}
