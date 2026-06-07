using System.Diagnostics;
using System.Diagnostics.Metrics;

using Netptune.Core.Enums;

namespace Netptune.Automation.Diagnostics;

internal static class Telemetry
{
    public const string SourceName = "Netptune.Automation";

    private static readonly ActivitySource ActivitySource = new(SourceName);
    private static readonly Meter Meter = new(SourceName);

    private static readonly Counter<long> RulesEvaluated = Meter.CreateCounter<long>(
        "netptune.automation.rules.evaluated",
        description: "Automation rules evaluated.");

    private static readonly Counter<long> RulesMatched = Meter.CreateCounter<long>(
        "netptune.automation.rules.matched",
        description: "Automation rules that matched an event or scheduled condition.");

    private static readonly Counter<long> RulesSkipped = Meter.CreateCounter<long>(
        "netptune.automation.rules.skipped",
        description: "Automation rules skipped before execution.");

    private static readonly Counter<long> RunsRequested = Meter.CreateCounter<long>(
        "netptune.automation.runs.requested",
        description: "Automation rule executions requested before idempotency filtering.");

    private static readonly Counter<long> RunsSkipped = Meter.CreateCounter<long>(
        "netptune.automation.runs.skipped",
        description: "Automation rule executions skipped.");

    private static readonly Counter<long> RunsSucceeded = Meter.CreateCounter<long>(
        "netptune.automation.runs.succeeded",
        description: "Automation runs marked as succeeded.");

    private static readonly Counter<long> RunsFailed = Meter.CreateCounter<long>(
        "netptune.automation.runs.failed",
        description: "Automation runs marked as failed.");

    private static readonly Counter<long> FlagsCreated = Meter.CreateCounter<long>(
        "netptune.automation.flags.created",
        description: "Flags created by automation actions.");

    private static readonly Counter<long> NotificationsPublished = Meter.CreateCounter<long>(
        "netptune.automation.notifications.published",
        description: "Notification events published by automation.");

    private static readonly Histogram<double> ExecutionDuration = Meter.CreateHistogram<double>(
        "netptune.automation.execution.duration",
        unit: "ms",
        description: "Automation execution duration.");

    public static Activity? StartActivity(string name, AutomationTriggerType triggerType)
    {
        var activity = ActivitySource.StartActivity(name);
        activity?.SetTag("automation.trigger_type", triggerType.ToString());

        return activity;
    }

    public static void RecordRulesEvaluated(AutomationTriggerType triggerType, int count)
    {
        if (count > 0) RulesEvaluated.Add(count, TriggerTags(triggerType));
    }

    public static void RecordRulesMatched(AutomationTriggerType triggerType, int count)
    {
        if (count > 0) RulesMatched.Add(count, TriggerTags(triggerType));
    }

    public static void RecordRulesSkipped(AutomationTriggerType triggerType, int count, string reason)
    {
        if (count <= 0) return;

        var tags = TriggerTags(triggerType);
        tags.Add("automation.skip_reason", reason);

        RulesSkipped.Add(count, tags);
    }

    public static void RecordRunsRequested(AutomationTriggerType triggerType, int count)
    {
        if (count > 0) RunsRequested.Add(count, TriggerTags(triggerType));
    }

    public static void RecordRunsSkipped(AutomationTriggerType triggerType, int count, string reason)
    {
        if (count <= 0) return;

        var tags = TriggerTags(triggerType);
        tags.Add("automation.skip_reason", reason);

        RunsSkipped.Add(count, tags);
    }

    public static void RecordRunResults(AutomationTriggerType triggerType, int succeeded, int failed)
    {
        if (succeeded > 0) RunsSucceeded.Add(succeeded, TriggerTags(triggerType));
        if (failed > 0) RunsFailed.Add(failed, TriggerTags(triggerType));
    }

    public static void RecordFlagsCreated(AutomationTriggerType triggerType, int count)
    {
        if (count > 0) FlagsCreated.Add(count, TriggerTags(triggerType));
    }

    public static void RecordNotificationsPublished(AutomationTriggerType triggerType, int count)
    {
        if (count > 0) NotificationsPublished.Add(count, TriggerTags(triggerType));
    }

    public static void RecordExecutionDuration(AutomationTriggerType triggerType, TimeSpan duration)
    {
        ExecutionDuration.Record(duration.TotalMilliseconds, TriggerTags(triggerType));
    }

    public static void MarkFailed(Activity? activity, Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.AddException(ex);
    }

    private static TagList TriggerTags(AutomationTriggerType triggerType)
    {
        var tags = new TagList
        {
            { "automation.trigger_type", triggerType.ToString() },
        };

        return tags;
    }
}
