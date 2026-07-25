using Microsoft.Extensions.Logging;

using Netptune.Automation.Common;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Execution;
using Netptune.Automation.Models;
using Netptune.Core.Enums;
using Netptune.Core.Events.Relations;
using Netptune.Core.Events.Tasks;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal abstract class TaskDependencyRuleMatcher
    : IEventRuleMatcher<TaskChangedMessage>, IEventRuleMatcher<TaskRelationChangedMessage>
{
    protected readonly INetptuneUnitOfWork UnitOfWork;

    private readonly ILogger Logger;

    public abstract AutomationTriggerType TriggerType { get; }

    protected abstract RelationCategory Category { get; }

    protected TaskDependencyRuleMatcher(INetptuneUnitOfWork unitOfWork, ILogger logger)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
    }

    protected abstract Task<List<int>> ResolveAffectedTaskIds(StatusTransition transition, int changedTaskId, CancellationToken cancellationToken);

    protected abstract Task<List<int>> ResolveAffectedTaskIds(TaskRelationChangedMessage message, CancellationToken cancellationToken);

    public async Task<List<PendingAutomationExecution>> Match(TaskChangedMessage message, CancellationToken cancellationToken)
    {
        var chainLimitReached = AutomationChainPolicy.HasReachedLimit(message.ChainDepth);

        if (chainLimitReached)
        {
            Telemetry.RecordRulesSkipped(TriggerType, 1, "chain_depth_limit");

            return [];
        }

        var transition = await ResolveStatusTransition(message, cancellationToken);

        if (transition is null)
        {
            return [];
        }

        var affectedTaskIds = await ResolveAffectedTaskIds(transition, message.TaskId, cancellationToken);
        var context = new DependencyTriggerContext
        {
            WorkspaceId = message.WorkspaceId,
            EventId = message.EventId,
            CorrelationId = message.CorrelationId ?? message.EventId,
            ActorUserId = message.ActorUserId,
            OccurredAt = message.OccurredAt,
            ChainDepth = message.ChainDepth,
        };

        return await BuildExecutions(context, affectedTaskIds, cancellationToken);
    }

    public async Task<List<PendingAutomationExecution>> Match(TaskRelationChangedMessage message, CancellationToken cancellationToken)
    {
        if (message.Category != Category)
        {
            return [];
        }

        var affectedTaskIds = await ResolveAffectedTaskIds(message, cancellationToken);
        var context = new DependencyTriggerContext
        {
            WorkspaceId = message.WorkspaceId,
            EventId = message.EventId,
            CorrelationId = message.EventId,
            ActorUserId = message.ActorUserId,
            OccurredAt = message.OccurredAt,
            ChainDepth = 0,
        };

        return await BuildExecutions(context, affectedTaskIds, cancellationToken);
    }

    private async Task<List<PendingAutomationExecution>> BuildExecutions(
        DependencyTriggerContext context,
        List<int> affectedTaskIds,
        CancellationToken cancellationToken)
    {
        if (affectedTaskIds.Count == 0)
        {
            return [];
        }

        var rules = await UnitOfWork.Automations.GetEnabledRulesForTrigger(
            TriggerType,
            context.WorkspaceId,
            cancellationToken);

        Telemetry.RecordRulesEvaluated(TriggerType, rules.Count);

        if (rules.Count == 0)
        {
            return [];
        }

        var tasks = await UnitOfWork.Tasks.GetAutomationTasks(affectedTaskIds, cancellationToken);

        if (tasks.Count == 0)
        {
            Logger.LogDebug("{TriggerType} automation found no affected tasks", TriggerType);

            return [];
        }

        var executions = new List<PendingAutomationExecution>();

        foreach (var rule in rules)
        {
            var matchingTasks = tasks.Where(task => AutomationRuleConditions.Match(rule, task));

            foreach (var task in matchingTasks)
            {
                executions.Add(new PendingAutomationExecution
                {
                    Rule = rule,
                    Task = task,
                    ExecutionUserId = rule.ExecutionUserId,
                    InitiatingUserId = context.ActorUserId,
                    IdempotencyKey = $"rule:{rule.Id}:task:{task.Id}:dependency:{context.EventId}",
                    TriggeredAt = context.OccurredAt,
                    CorrelationId = context.CorrelationId,
                    CausationEventId = context.EventId,
                    ChainDepth = context.ChainDepth,
                });
            }
        }

        Telemetry.RecordRulesMatched(TriggerType, executions.Count);

        return executions;
    }

    private async Task<StatusTransition?> ResolveStatusTransition(
        TaskChangedMessage message,
        CancellationToken cancellationToken)
    {
        var statusChange = message.Changes.FirstOrDefault(change => change.Field == TaskChangeField.Status);

        if (statusChange is null)
        {
            return null;
        }

        var previousStatusId = ParseStatusId(statusChange.OldValue);
        var currentStatusId = ParseStatusId(statusChange.NewValue);

        if (previousStatusId is null || currentStatusId is null)
        {
            return null;
        }

        var categories = await UnitOfWork.Statuses.GetCategories(
            [previousStatusId.Value, currentStatusId.Value],
            cancellationToken);

        var hasPreviousCategory = categories.TryGetValue(previousStatusId.Value, out var previousCategory);
        var hasCurrentCategory = categories.TryGetValue(currentStatusId.Value, out var currentCategory);

        if (!hasPreviousCategory || !hasCurrentCategory)
        {
            return null;
        }

        return new StatusTransition(previousCategory, currentCategory);
    }

    private static int? ParseStatusId(string? value)
    {
        return int.TryParse(value, out var statusId) ? statusId : null;
    }
}

internal sealed record StatusTransition(StatusCategory Previous, StatusCategory Current)
{
    public bool BecameComplete => Previous != StatusCategory.Done && Current == StatusCategory.Done;

    public bool BecameIncomplete => Previous == StatusCategory.Done && Current != StatusCategory.Done;
}

internal sealed record DependencyTriggerContext
{
    public required int WorkspaceId { get; init; }

    public required Guid EventId { get; init; }

    public required Guid CorrelationId { get; init; }

    public string? ActorUserId { get; init; }

    public required DateTime OccurredAt { get; init; }

    public int ChainDepth { get; init; }
}
